using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using XdtHtml.Properties;
using HtmlAgilityPack;

namespace XdtHtml
{
    public enum XPathAxis {
        Child,
        Descendant,
        Parent,
        Ancestor,
        FollowingSibling,
        PrecedingSibling,
        Following,
        Preceding,
        Self,
        DescendantOrSelf,
        AncestorOrSelf,
    }

    public abstract class Locator
    {
        #region private data members
        private string argumentString = null;
        private IList<string> arguments = null;
        private string parentPath = null;
        private HtmlElementContext context = null;
        private HtmlTransformationLogger logger = null;
        #endregion

        protected virtual string ParentPath {
            get {
                return parentPath;
            }
        }

        protected HtmlNode CurrentElement {
            get {
                return context.Element;
            }
        }

        virtual protected string NextStepNodeTest {
            get {
                //if (!String.IsNullOrEmpty(CurrentElement.NamespaceURI) && String.IsNullOrEmpty(CurrentElement.Prefix)) {
                //    return String.Concat("_defaultNamespace:", CurrentElement.LocalName);
                //}
                //else {
                    return CurrentElement.Name;
                //}
            }
        }
        virtual protected XPathAxis NextStepAxis {
            get {
                return XPathAxis.Child;
            }
        }

        protected virtual string ConstructPath() {
            return AppendStep(ParentPath, NextStepAxis, NextStepNodeTest, ConstructPredicate());
        }

        protected string AppendStep(string basePath, string stepNodeTest) {
            return AppendStep(basePath, XPathAxis.Child, stepNodeTest, String.Empty);
        }

        protected string AppendStep(string basePath, XPathAxis stepAxis, string stepNodeTest) {
            return AppendStep(basePath, stepAxis, stepNodeTest, String.Empty);
        }

        protected string AppendStep(string basePath, string stepNodeTest, string predicate) {
            return AppendStep(basePath, XPathAxis.Child, stepNodeTest, predicate);
        }

        protected string AppendStep(string basePath, XPathAxis stepAxis, string stepNodeTest, string predicate) {
            return String.Concat(
                stepAxis == XPathAxis.Child && stepNodeTest.StartsWith("/") ? basePath : EnsureTrailingSlash(basePath), 
                GetAxisString(stepAxis), 
                stepNodeTest, 
                EnsureBracketedPredicate(predicate));
        }

        protected virtual string ConstructPredicate() {
            return String.Empty;
        }

        protected HtmlTransformationLogger Log {
            get {
                if (logger == null) {
                    logger = context.GetService<HtmlTransformationLogger>();
                    if (logger != null) {
                        logger.CurrentReferenceAttribute = context.LocatorAttribute;
                    }
                }
                return logger;
            }
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

        protected void EnsureArguments() {
            EnsureArguments(1);
        }

        protected void EnsureArguments(int min) {
            if (Arguments == null || Arguments.Count < min) {
                throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_RequiresMinimumArguments, GetType().Name, min));
            }
        }

        protected void EnsureArguments(int min, int max) {
            Debug.Assert(min <= max);
            if (min == max) {
                if (Arguments == null || Arguments.Count != min) {
                    throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_RequiresExactArguments, GetType().Name, min));
                }
            }

            EnsureArguments(min);

            if (Arguments.Count > max) {
                throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TooManyArguments, GetType().Name));
            }
        }

        internal string ConstructPath(string parentPath, HtmlElementContext context, string argumentString) {
            Debug.Assert(this.parentPath == null && this.context == null && this.argumentString == null,
                "Do not call ConstructPath recursively");

            string resultPath = String.Empty;

            if (this.parentPath == null && this.context == null && this.argumentString == null) {
                try {
                    this.parentPath = parentPath;
                    this.context = context;
                    this.argumentString = argumentString;

                    resultPath = ConstructPath();
                }
                finally {
                    this.parentPath = null;
                    this.context = null;
                    this.argumentString = null;
                    this.arguments = null;

                    ReleaseLogger();
                }
            }

            return resultPath;
        }

        internal string ConstructParentPath(string parentPath, HtmlElementContext context, string argumentString) {
            Debug.Assert(this.parentPath == null && this.context == null && this.argumentString == null,
                "Do not call ConstructPath recursively");

            string resultPath = String.Empty;

            if (this.parentPath == null && this.context == null && this.argumentString == null) {
                try {
                    this.parentPath = parentPath;
                    this.context = context;
                    this.argumentString = argumentString;

                    resultPath = ParentPath;
                }
                finally {
                    this.parentPath = null;
                    this.context = null;
                    this.argumentString = null;
                    this.arguments = null;

                    ReleaseLogger();
                }
            }

            return resultPath;
        }

        private void ReleaseLogger() {
            if (logger != null) {
                logger.CurrentReferenceAttribute = null;
                logger = null;
            }
        }

        private string GetAxisString(XPathAxis stepAxis) {
            switch (stepAxis) {
                case XPathAxis.Child:
                    return String.Empty;
                case XPathAxis.Descendant:
                    return "descendant::";
                case XPathAxis.Parent:
                    return "parent::";
                case XPathAxis.Ancestor:
                    return "ancestor::";
                case XPathAxis.FollowingSibling:
                    return "following-sibling::";
                case XPathAxis.PrecedingSibling:
                    return "preceding-sibling::";
                case XPathAxis.Following:
                    return "following::";
                case XPathAxis.Preceding:
                    return "preceding::";
                case XPathAxis.Self:
                    return "self::";
                case XPathAxis.DescendantOrSelf:
                    return "/";
                case XPathAxis.AncestorOrSelf:
                    return "ancestor-or-self::";
                default:
                    Debug.Fail("There should be no XPathAxis enum value that isn't handled in this switch statement");
                    return String.Empty;
            }
        }

        private string EnsureTrailingSlash(string basePath) {
            if (!basePath.EndsWith("/", StringComparison.Ordinal)) {
                basePath = String.Concat(basePath, "/");
            }

            return basePath;
        }

        private string EnsureBracketedPredicate(string predicate) {
            if (String.IsNullOrEmpty(predicate)) {
                return String.Empty;
            }
            else {
                if (!predicate.StartsWith("[", StringComparison.Ordinal)) {
                    predicate = String.Concat(String.Concat("[", predicate), ']');
                }

                return predicate;
            }
        }
    }
}
