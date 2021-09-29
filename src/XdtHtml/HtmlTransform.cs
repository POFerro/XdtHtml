using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using XdtHtml.Properties;
using HtmlAgilityPack;

namespace XdtHtml
{
    public enum MissingTargetMessage
    {
        None,
        Information,
        Warning,
        Error,
    }

    [Flags]
    public enum TransformFlags
    {
        None = 0,
        ApplyTransformToAllTargetNodes = 1,
        UseParentAsTargetNode = 2,
    }


    public abstract class Transform
    {
        #region private data members
        private MissingTargetMessage missingTargetMessage;
        private bool applyTransformToAllTargetNodes;
        private bool useParentAsTargetNode;

        private HtmlTransformationLogger logger = null;
        private HtmlElementContext context = null;
        private HtmlNode currentTransformNode = null;
        private HtmlNode currentTargetNode = null;

        private string argumentString = null;
        private IList<string> arguments = null;
        #endregion

        protected Transform()
            : this(TransformFlags.None) {
        }

        protected Transform(TransformFlags flags)
            : this(flags, MissingTargetMessage.Warning) {
        }

        protected Transform(TransformFlags flags, MissingTargetMessage message) {
            this.missingTargetMessage = message;
            this.applyTransformToAllTargetNodes = (flags & TransformFlags.ApplyTransformToAllTargetNodes) == TransformFlags.ApplyTransformToAllTargetNodes;
            this.useParentAsTargetNode = (flags & TransformFlags.UseParentAsTargetNode) == TransformFlags.UseParentAsTargetNode;
        }

        protected bool ApplyTransformToAllTargetNodes {
            get {
                return applyTransformToAllTargetNodes;
            }
            set {
                applyTransformToAllTargetNodes = value;
            }
        }

        protected bool UseParentAsTargetNode {
            get {
                return useParentAsTargetNode;
            }
            set {
                useParentAsTargetNode = value;
            }
        }

        protected MissingTargetMessage MissingTargetMessage {
            get {
                return missingTargetMessage;
            }
            set {
                missingTargetMessage = value;
            }
        }

        public static MessageType TransformMessageType { get; set; } = MessageType.Verbose;


        protected abstract void Apply();

        protected HtmlNode TransformNode {
            get {
                if (currentTransformNode == null) {
                    return context.TransformNode;
                }
                else {
                    return currentTransformNode;
                }
            }
        }

        protected HtmlNode TargetNode {
            get {
                if (currentTargetNode == null) {
                    foreach (HtmlNode targetNode in TargetNodes) {
                        return targetNode;
                    }
                }
                return currentTargetNode;
            }
        }

        protected HtmlNodeCollection TargetNodes {
            get {
                if (UseParentAsTargetNode) {
                    return context.TargetParents;
                }
                else {
                    return context.TargetNodes;
                }
            }
        }


        protected HtmlNodeCollection TargetChildNodes
        {
            get
            {
                return context.TargetNodes;
            }
        }

        protected HtmlTransformationLogger Log {
            get {
                if (logger == null) {
                    logger = context.GetService<HtmlTransformationLogger>();
                    if (logger != null) {
                        logger.CurrentReferenceAttribute = context.TransformAttribute;
                    }
                }
                return logger;
            }
        }



        protected T GetService<T>() where T : class
        {
            return context.GetService<T>();
        }

        protected string ArgumentString {
            get {
                return argumentString;
            }
        }

        protected IList<string> Arguments {
            get {
                if (arguments == null && argumentString != null) {
                    arguments = HtmlArgumentUtility.SplitArguments(argumentString);
                }
                return arguments;
            }
        }

        private string TransformNameLong {
            get {
                if (context.HasLineInfo) {
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_TransformNameFormatLong, TransformName, context.TransformLineNumber, context.TransformLinePosition);
                }
                else {
                    return TransformNameShort;
                }
            }
        }

        internal string TransformNameShort {
            get {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_TransformNameFormatShort, TransformName);
            }
        }

        private string TransformName {
            get {
                return GetType().Name;
            }
        }

        internal void Execute(HtmlElementContext context, string argumentString) {
            Debug.Assert(this.context == null && this.argumentString == null, "Don't call Execute recursively");
            Debug.Assert(this.logger == null, "Logger wasn't released from previous execution");

            if (this.context == null && this.argumentString == null) {
                bool error = false;
                bool startedSection = false;

                try {
                    this.context = context;
                    this.argumentString = argumentString;
                    this.arguments = null;

                    if (ShouldExecuteTransform()) {
                        startedSection = true;

                        Log.StartSection(TransformMessageType, Resources.XMLTRANSFORMATION_TransformBeginExecutingMessage, TransformNameLong);
                        Log.LogMessage(TransformMessageType, Resources.XMLTRANSFORMATION_TransformStatusXPath, context.XPath);

                        if (ApplyTransformToAllTargetNodes) {
                            ApplyOnAllTargetNodes();
                        }
                        else {
                            ApplyOnce();
                        }
                    }
                }
                catch (Exception ex) {
                    error = true;
                    if (context.TransformAttribute != null) {
                        Log.LogErrorFromException(HtmlNodeException.Wrap(ex, context.TransformAttribute));
                    }
                    else {
                        Log.LogErrorFromException(ex);
                    }
                }
                finally {
                    if (startedSection) {
                        if (error) {
                            Log.EndSection(TransformMessageType, Resources.XMLTRANSFORMATION_TransformErrorExecutingMessage, TransformNameShort);
                        }
                        else {
                            Log.EndSection(TransformMessageType, Resources.XMLTRANSFORMATION_TransformEndExecutingMessage, TransformNameShort);
                        }
                    }
                    else {
                        Log.LogMessage(MessageType.Normal, Resources.XMLTRANSFORMATION_TransformNotExecutingMessage, TransformNameLong);
                    }

                    this.context = null;
                    this.argumentString = null;
                    this.arguments = null;

                    ReleaseLogger();
                }
            }
        }

        private void ReleaseLogger() {
            if (logger != null) {
                logger.CurrentReferenceAttribute = null;
                logger = null;
            }
        }

        private bool ApplyOnAllTargetNodes() {
            bool error = false;
            HtmlNode originalTransformNode = TransformNode;

            foreach (HtmlNode node in TargetNodes) {
                try {
                    currentTargetNode = node;
                    currentTransformNode = originalTransformNode.Clone();

                    ApplyOnce();
                }
                catch (Exception ex) {
                    Log.LogErrorFromException(ex);
                    error = true;
                }
            }

            currentTargetNode = null;

            return error;
        }

        private void ApplyOnce() {
            WriteApplyMessage(TargetNode);
            Apply();
        }

        private void WriteApplyMessage(HtmlNode targetNode) {
            //if (lineInfo != null) {
                Log.LogMessage(TransformMessageType, Resources.XMLTRANSFORMATION_TransformStatusApplyTarget, targetNode.Name, targetNode.Line, targetNode.LinePosition);
            //}
            //else {
            //    Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformStatusApplyTargetNoLineInfo, targetNode.Name);
            //}
        }

        private bool ShouldExecuteTransform() {
            return HasRequiredTarget();
        }

        private bool HasRequiredTarget() {
            bool hasRequiredTarget;
            bool existedInOriginal;
            HtmlElementContext matchFailureContext;

            if (UseParentAsTargetNode) {
                hasRequiredTarget = context.HasTargetParent(out matchFailureContext, out existedInOriginal);
            }
            else {
                hasRequiredTarget = context.HasTargetNode(out matchFailureContext, out existedInOriginal);
            }

            if (!hasRequiredTarget) {
                HandleMissingTarget(matchFailureContext, existedInOriginal);
                return false;
            }

            return true;
        }

        private void HandleMissingTarget(HtmlElementContext matchFailureContext, bool existedInOriginal) {
            string messageFormat = existedInOriginal
                ? Resources.XMLTRANSFORMATION_TransformSourceMatchWasRemoved
                : Resources.XMLTRANSFORMATION_TransformNoMatchingTargetNodes;

            string message = string.Format(System.Globalization.CultureInfo.CurrentCulture,messageFormat, matchFailureContext.XPath);
            switch(MissingTargetMessage) {
                case MissingTargetMessage.None:
                    Log.LogMessage(MessageType.Verbose, message);
                    break;
                case MissingTargetMessage.Information:
                    Log.LogMessage(MessageType.Normal, message);
                    break;
                case MissingTargetMessage.Warning:
                    Log.LogWarning(matchFailureContext.Node, message);
                    break;
                case MissingTargetMessage.Error:
                    throw new HtmlNodeException(message, matchFailureContext.Node);
            }
        }
    }
}
