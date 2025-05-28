using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using RegularExpressions = System.Text.RegularExpressions;
using XdtHtml.Properties;
using HtmlAgilityPack;
using System.Linq;

namespace XdtHtml
{
    internal static class CommonErrors
    {
        internal static void ExpectNoArguments(HtmlTransformationLogger log, string transformName, string argumentString) {
            if (!String.IsNullOrEmpty(argumentString)) {
                log.LogWarning(Resources.XMLTRANSFORMATION_TransformDoesNotExpectArguments, transformName);
            }
        }

        internal static void WarnIfMultipleTargets(HtmlTransformationLogger log, string transformName, HtmlNodeCollection targetNodes, bool applyTransformToAllTargets) {
            Debug.Assert(applyTransformToAllTargets == false);

            if (targetNodes.Count > 1) {
                log.LogWarning(Resources.XMLTRANSFORMATION_TransformOnlyAppliesOnce, transformName);
            }
        }
    }

    internal class Replace : Transform
    {
        protected override void Apply() {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);
            CommonErrors.WarnIfMultipleTargets(Log, TransformNameShort, TargetNodes, ApplyTransformToAllTargetNodes);

            HtmlNode parentNode = TargetNode.ParentNode;
            parentNode.ReplaceChild(
                TransformNode.AdjustIndent(TargetNode.GetIndentationWithNewline(), TransformNodeIndentationWithNewline),
                TargetNode);

            Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageReplace, TargetNode.Name);
        }
    }

    internal class ReplaceElement : Transform
    {
        public ReplaceElement()
            : base(TransformFlags.UseParentAsTargetNode, MissingTargetMessage.Error)
        {
        }

        private HtmlNode elementToReplace = null;

        protected HtmlNode ElementToReplace
        {
            get
            {
                if (elementToReplace == null)
                {
                    if (Arguments == null || Arguments.Count == 0)
                    {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertMissingArgument, GetType().Name));
                    }
                    else if (Arguments.Count > 1)
                    {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertTooManyArguments, GetType().Name));
                    }
                    else
                    {
                        string xpath = Arguments[0];
                        HtmlNodeCollection siblings = TargetNode.SelectNodes(xpath);
                        if (siblings.Count == 0)
                        {
                            throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertBadXPath, xpath));
                        }
                        else if (siblings.Count > 1)
                        {
                            throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TransformOnlyAppliesOnce, GetType().Name));
                        }
                        else
                        {
                            elementToReplace = siblings[0];
                        }
                    }
                }

                return elementToReplace;
            }
        }

        protected override void Apply()
        {
            CommonErrors.WarnIfMultipleTargets(Log, TransformNameShort, TargetNodes, ApplyTransformToAllTargetNodes);

            TargetNode.ReplaceChild(
                TransformNode.AdjustIndent(ElementToReplace.GetIndentationWithNewline(), TransformNodeIndentationWithNewline),
                ElementToReplace);

            Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageReplace, ElementToReplace.Name);
        }
    }

    internal class Remove : Transform
    {
        protected override void Apply()
        {
            CommonErrors.WarnIfMultipleTargets(Log, TransformNameShort, TargetNodes, ApplyTransformToAllTargetNodes);

            RemoveNode();
        }

        protected void RemoveNode()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);

            foreach (var node in TargetNode.GetNodeWithPreamble().ToList())
            {
                node.Remove();
            }

            Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageRemove, TargetNode.Name);
        }
    }

    internal class RemoveIfExists : Remove
    {
        public RemoveIfExists()
        {
            this.MissingTargetMessage = MissingTargetMessage.None;
        }
    }

    internal class RemoveAll : Remove
    {
        public RemoveAll() {
            ApplyTransformToAllTargetNodes = true;
        }

        protected override void Apply() {
            RemoveNode();
        }
    }
    internal class RemoveAllIfExists : RemoveAll
    {
        public RemoveAllIfExists()
        {
            this.MissingTargetMessage = MissingTargetMessage.None;
        }
    }

    internal class Insert : Transform
    {
        public Insert()
            : base(TransformFlags.UseParentAsTargetNode, MissingTargetMessage.Error) {
        }

        protected override void Apply() {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);

            var lastElement = TargetNode.ChildNodes.LastOrDefault(n => n.NodeType == HtmlNodeType.Element);

            IEnumerable<HtmlNode> indentedNodes;
            if (lastElement != null)
                indentedNodes = TransformNodePreamble.Append(TransformNode).AdjustIndent(lastElement.GetIndentationWithNewline(), TransformNodeIndentationWithNewline).Concat(TargetNode.GetEndTagPreamble().Select(n => n.CloneNode(true))).ToList();
            else
                indentedNodes = TransformNodePreamble.Append(TransformNode).AdjustParentIndent(TargetNode.GetIndentationWithNewline(), TransformNodeIndentationWithNewline, TransformNodeIndentationFromParent).Concat(TargetNode.GetEndTagPreamble().Select(n => n.CloneNode(true))).ToList();

            TargetNode.AppendChild(indentedNodes);

            Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name);
        }
    }

    internal class InsertIfMissing : Insert
    {
        protected override void Apply()
        {
            CommonErrors.ExpectNoArguments(Log, TransformNameShort, ArgumentString);
            if (this.TargetChildNodes == null || this.TargetChildNodes.Count == 0)
            {
                base.Apply();
            }
        }
    }



    internal abstract class InsertBase : Transform
    {
        internal InsertBase()
            : base(TransformFlags.UseParentAsTargetNode, MissingTargetMessage.Error) {
        }

        private HtmlNode siblingElement = null;
        private HtmlNodeCollection probeTargetElements = null;

        protected HtmlNode SiblingElement {
            get {
                if (siblingElement == null) {
                    if (Arguments == null || Arguments.Count == 0) {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_InsertMissingArgument, GetType().Name));
                    }
                    else if (Arguments.Count > 2) {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_InsertTooManyArguments, GetType().Name));
                    }
                    else {
                        string xpath = Arguments[0];
                        HtmlNodeCollection siblings = TargetNode.SelectNodes(xpath);
                        if (siblings.Count == 0) {
                            throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_InsertBadXPath, xpath));
                        }
                        else {
                            siblingElement = siblings[0] as HtmlNode;
                            if (siblingElement == null) {
                                throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_InsertBadXPathResult, xpath));
                            }
                        }
                    }
                }

                return siblingElement;
            }
        }

        protected HtmlNodeCollection ProbeTargetElements
        {
            get
            {
                if (probeTargetElements == null)
                {
                    if (Arguments == null || Arguments.Count == 0)
                    {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertMissingArgument, GetType().Name));
                    }
                    else if (Arguments.Count > 2)
                    {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertTooManyArguments, GetType().Name));
                    }
                    else if (Arguments.Count > 1)
                    {
                        string xpath = Arguments[1];
                        probeTargetElements = TargetNode.SelectNodes(xpath);
                    }
                    else
                    {
                        probeTargetElements = TargetChildNodes;
                    }
                }

                return probeTargetElements;
            }
        }
    }

    internal class InsertAfter : InsertBase
    {
        protected override void Apply() {
            SiblingElement.ParentNode.InsertAfter(TransformNodePreamble.Append(TransformNode).AdjustIndent(SiblingElement.GetIndentationWithNewline(), TransformNodeIndentationWithNewline).ToList(), SiblingElement);

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }
    internal class InsertAfterIfMissing : InsertAfter
    {
        protected override void Apply()
        {
            if (this.ProbeTargetElements == null || this.ProbeTargetElements.Count == 0)
            {
                base.Apply();
            }
        }
    }

    internal class InsertBefore : InsertBase
    {
        protected override void Apply() {
            SiblingElement.ParentNode.InsertBefore(new[] { TransformNode.AdjustIndent(SiblingElement.GetIndentationWithNewline(), TransformNodeIndentationWithNewline) }.Concat(SiblingElement.GetNodePreamble()).Select(n => n.CloneNode(true)).ToList(), SiblingElement);

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }
    internal class InsertBeforeIfMissing : InsertBefore
    {
        protected override void Apply()
        {
            if (this.ProbeTargetElements == null || this.ProbeTargetElements.Count == 0)
            {
                base.Apply();
            }
        }
    }

    internal class InsertInto: InsertIntoBegining
    {
    }

    internal class InsertIntoBegining : InsertBase
    {
        protected override void Apply()
        {
            var firstElement = SiblingElement.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlNodeType.Element);

            IEnumerable<HtmlNode> indentedNodes;
            if (firstElement != null)
                indentedNodes = TransformNodePreamble.Append(TransformNode).AdjustIndent(firstElement.GetIndentationWithNewline(), TransformNodeIndentationWithNewline).ToList();
            else
                indentedNodes = TransformNodePreamble.Append(TransformNode).AdjustParentIndent(SiblingElement.GetIndentationWithNewline(), TransformNodeIndentationWithNewline, TransformNodeIndentationFromParent).ToList();
            
            SiblingElement.PrependChild(indentedNodes);

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }
    internal class InsertIntoBeginingIfMissing : InsertIntoBegining
    {
        protected override void Apply()
        {
            if (this.ProbeTargetElements == null || this.ProbeTargetElements.Count == 0)
            {
                base.Apply();
            }
        }
    }

    internal class InsertIntoEnd : InsertBase
    {
        protected override void Apply()
        {
            var originalEndTagPreamble = SiblingElement.GetEndTagPreamble().Select(n => n.CloneNode(true)).ToList();
            SiblingElement.GetEndTagPreamble().OfType<HtmlTextNode>().ToList().ForEach(n => n.Text = n.Text.Trim(' '));

            var lastElement = SiblingElement.ChildNodes.LastOrDefault(n => n.NodeType == HtmlNodeType.Element);

            IEnumerable<HtmlNode> indentedNodes;
            if (lastElement != null)
                indentedNodes = TransformNodePreamble.Append(TransformNode).AdjustIndent(lastElement.GetIndentationWithNewline(), TransformNodeIndentationWithNewline).Concat(originalEndTagPreamble).ToList();
            else
                indentedNodes = TransformNodePreamble.Append(TransformNode).AdjustParentIndent(SiblingElement.GetIndentationWithNewline(), TransformNodeIndentationWithNewline, TransformNodeIndentationFromParent).Concat(originalEndTagPreamble).ToList();

            SiblingElement.AppendChild(indentedNodes);

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }
    internal class InsertIntoEndIfMissing : InsertIntoEnd
    {
        protected override void Apply()
        {
            if (this.ProbeTargetElements == null || this.ProbeTargetElements.Count == 0)
            {
                base.Apply();
            }
        }
    }

    internal abstract class MoveToBase : Transform
    {
        internal MoveToBase()
            : base(TransformFlags.None, MissingTargetMessage.Error)
        {
        }

        private HtmlNode siblingElement = null;

        protected HtmlNode SiblingElement
        {
            get
            {
                if (siblingElement == null)
                {
                    if (Arguments == null || Arguments.Count == 0)
                    {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertMissingArgument, GetType().Name));
                    }
                    else if (Arguments.Count > 1)
                    {
                        throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertTooManyArguments, GetType().Name));
                    }
                    else
                    {
                        string xpath = Arguments[0];
                        HtmlNodeCollection siblings = TargetNode.ParentNode.SelectNodes(xpath);
                        if (siblings.Count == 0)
                        {
                            throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertBadXPath, xpath));
                        }
                        else
                        {
                            siblingElement = siblings[0] as HtmlNode;
                            if (siblingElement == null)
                            {
                                throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_InsertBadXPathResult, xpath));
                            }
                        }
                    }
                }

                return siblingElement;
            }
        }
    }
    internal class MoveToAfter : MoveToBase
    {
        public MoveToAfter()
        {
            this.UseParentAsTargetNode = false;
        }

        protected override void Apply()
        {
            SiblingElement.ParentNode.InsertAfter(TargetNode.GetNodeWithPreamble().AdjustIndent(SiblingElement, false).ToList().Select(n => n.CloneNode(true)), SiblingElement);
            foreach (var node in TargetNode.GetNodeWithPreamble().ToList())
            {
                node.Remove();
            }

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }

    internal class MoveToBefore : MoveToBase
    {
        public MoveToBefore()
        {
            this.UseParentAsTargetNode = false;
        }

        protected override void Apply()
        {
            SiblingElement.ParentNode.InsertBefore(new[] { TargetNode.AdjustIndent(SiblingElement) }.Concat(SiblingElement.GetNodePreamble()).Select(n => n.CloneNode(true)).ToList(), SiblingElement);
            foreach (var node in TargetNode.GetNodeWithPreamble().ToList())
            {
                node.Remove();
            }

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }

    internal class MoveInto : MoveIntoBegining
    {
    }

    internal class MoveIntoBegining : MoveToBase
    {
        public MoveIntoBegining()
        {
            this.UseParentAsTargetNode = false;
            this.ApplyTransformToAllTargetNodes = true;
        }
        protected override void Apply()
        {
            var firstElement = SiblingElement.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlNodeType.Element);

            IEnumerable<HtmlNode> indentedNodes;
            if (firstElement != null)
                indentedNodes = TargetNode.GetNodeWithPreamble().AdjustIndent(firstElement, false).ToList().Select(n => n.CloneNode(true));
            else
                indentedNodes = TargetNode.GetNodeWithPreamble().AdjustParentIndent(SiblingElement, false).ToList().Select(n => n.CloneNode(true));

            SiblingElement.PrependChild(indentedNodes);

            foreach (var node in TargetNode.GetNodeWithPreamble().ToList())
            {
                node.Remove();
            }

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }

    internal class MoveIntoEnd : MoveToBase
    {
        public MoveIntoEnd()
        {
            this.UseParentAsTargetNode = false;
            this.ApplyTransformToAllTargetNodes = true;
        }
        protected override void Apply()
        {
            var originalEndTagPreamble = SiblingElement.GetEndTagPreamble().Select(n => n.CloneNode(true)).ToList();
            SiblingElement.GetEndTagPreamble().OfType<HtmlTextNode>().ToList().ForEach(n => n.Text = n.Text.Trim(' '));

            var lastElement = SiblingElement.ChildNodes.LastOrDefault(n => n.NodeType == HtmlNodeType.Element);

            IEnumerable<HtmlNode> indentedNodes;
            if (lastElement != null)
                indentedNodes = TargetNode.GetNodeWithPreamble().AdjustIndent(lastElement, false).ToList().Select(n => n.CloneNode(true)).Concat(originalEndTagPreamble);
            else
                indentedNodes = TargetNode.GetNodeWithPreamble().AdjustParentIndent(SiblingElement, false).ToList().Select(n => n.CloneNode(true)).Concat(originalEndTagPreamble);

            SiblingElement.AppendChild(indentedNodes);

            foreach (var node in TargetNode.GetNodeWithPreamble().ToList())
            {
                node.Remove();
            }

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.XMLTRANSFORMATION_TransformMessageInsert, TransformNode.Name));
        }
    }

    public class SetAttributes : AttributeTransform
    {
        protected override void Apply() {
            foreach (HtmlAttribute transformAttribute in TransformAttributes) {
                HtmlAttribute targetAttribute = TargetNode.Attributes[transformAttribute.Name];
                if (targetAttribute != null) {
                    targetAttribute.Value = transformAttribute.Value;
                }
                else {
                    TargetNode.Attributes.Append(transformAttribute.Clone());
                }

                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageSetAttribute, transformAttribute.Name);
            }

            if (TransformAttributes.Count > 0) {
                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageSetAttributes, TransformAttributes.Count);
            }
            else {
                Log.LogWarning(Resources.XMLTRANSFORMATION_TransformMessageNoSetAttributes);
            }
        }
    }

    public class CommentOut : Transform
    {
        protected override void Apply()
        {
            HtmlNode parentNode = TargetNode.ParentNode;
            HtmlDocument document = TargetNode.OwnerDocument;
            if (parentNode == null || document == null)
            {
                throw new HtmlTransformationException(Resources.XMLTRANSFORMATION_InvalidCommentOutTarget);
            }

            string outerHtml = $" {TargetNode.OuterHtml} ";
            HtmlCommentNode comment = document.CreateComment(outerHtml);
            parentNode.InsertAfter(comment, TargetNode);
            parentNode.RemoveChild(TargetNode);

            Log.LogMessage(MessageType.Verbose, string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_TransformMessageCommentOut, TargetNode.Name));
        }
    }
    

    public class SetTokenizedAttributeStorage
    {
        public List<Dictionary<string, string>> DictionaryList { get; set; }
        public string TokenFormat { get; set; }
        public bool EnableTokenizeParameters { get; set; }
        public bool UseXpathToFormParameter { get; set; }
        public SetTokenizedAttributeStorage() : this(4) { }
        public SetTokenizedAttributeStorage(int capacity)
        {
            DictionaryList = new List<Dictionary<string, string>>(capacity);
            TokenFormat = string.Concat("$(ReplacableToken_#(", SetTokenizedAttributes.ParameterAttribute, ")_#(", SetTokenizedAttributes.TokenNumber, "))");
            EnableTokenizeParameters = false;
            UseXpathToFormParameter = true;
        }
    }

    /// <summary>
    /// Utility class to Transform the SetAttribute to replace token
    /// 1. if it trigger by the regular TransformXml task, it only replace the $(name) from the parent node
    /// 2. If it trigger by the TokenizedTransformXml task, it replace $(name) then parse the declareation of the parameter
    /// </summary>
    public class SetTokenizedAttributes : AttributeTransform
    {

        private SetTokenizedAttributeStorage storageDictionary = null;
        private bool fInitStorageDictionary = false;
        public static readonly string Token = "Token";
        public static readonly string TokenNumber = "TokenNumber";
        public static readonly string XPathWithIndex = "XPathWithIndex";
        public static readonly string ParameterAttribute = "Parameter";
        public static readonly string XpathLocator = "XpathLocator";
        public static readonly string XPathWithLocator = "XPathWithLocator";

        private HtmlAttribute tokenizeValueCurrentXmlAttribute = null;

    
        protected SetTokenizedAttributeStorage TransformStorage
        {
            get
            {
                if (storageDictionary == null && !fInitStorageDictionary)
                {
                    storageDictionary = GetService<SetTokenizedAttributeStorage>();
                    fInitStorageDictionary = true;
                }
                return storageDictionary;
            }
        }

        protected override void Apply()
        {
            bool fTokenizeParameter = false;
            SetTokenizedAttributeStorage storage = TransformStorage;
            List<Dictionary<string, string> > parameters = null;

            if (storage != null)
            {
                fTokenizeParameter = storage.EnableTokenizeParameters;
                if (fTokenizeParameter)
                {
                    parameters = storage.DictionaryList;
                }
            }

            foreach (HtmlAttribute transformAttribute in TransformAttributes)
            {
                HtmlAttribute targetAttribute = TargetNode.Attributes[transformAttribute.Name];

                string newValue = TokenizeValue(targetAttribute, transformAttribute, fTokenizeParameter, parameters);

                if (targetAttribute != null)
                {
                    targetAttribute.Value = newValue;
                }
                else
                {
                    HtmlAttribute newAttribute = transformAttribute.Clone();
                    newAttribute.Value = newValue;
                    TargetNode.Attributes.Append(newAttribute);
                }

                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageSetAttribute, transformAttribute.Name);
            }

            if (TransformAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageSetAttributes, TransformAttributes.Count);
            }
            else
            {
                Log.LogWarning(Resources.XMLTRANSFORMATION_TransformMessageNoSetAttributes);
            }
        }


        static private RegularExpressions.Regex s_dirRegex = null;
        static private RegularExpressions.Regex s_parentAttribRegex = null;
        static private RegularExpressions.Regex s_tokenFormatRegex = null;

        // Directory registrory
        static internal RegularExpressions.Regex DirRegex
        {
            get
            {
                if (s_dirRegex == null)
                {
                    s_dirRegex = new RegularExpressions.Regex(@"\G\{%(\s*(?<attrname>\w+(?=\W))(\s*(?<equal>=)\s*'(?<attrval>[^']*)'|\s*(?<equal>=)\s*(?<attrval>[^\s%>]*)|(?<equal>)(?<attrval>\s*?)))*\s*?%\}");
                }
                return s_dirRegex;
            }
        }

        static internal RegularExpressions.Regex ParentAttributeRegex
        {
            get
            {
                if (s_parentAttribRegex == null)
                {
                    s_parentAttribRegex = new RegularExpressions.Regex(@"\G\$\((?<tagname>[\w:\.]+)\)"); 
                }
                return s_parentAttribRegex;
            }
        }

        static internal RegularExpressions.Regex TokenFormatRegex
        {
            get
            {
                if (s_tokenFormatRegex == null)
                {
                    s_tokenFormatRegex = new RegularExpressions.Regex(@"\G\#\((?<tagname>[\w:\.]+)\)");
                }
                return s_tokenFormatRegex;
            }
        }

        protected delegate string GetValueCallback(string key);

        protected string GetAttributeValue(string attributeName)
        {
            string dataValue = null;
            HtmlAttribute sourceAttribute = TargetNode.Attributes[attributeName];
            if (sourceAttribute == null)
            {
                if (string.Compare(attributeName, tokenizeValueCurrentXmlAttribute.Name, StringComparison.OrdinalIgnoreCase) != 0)
                {   // if it is other attributename, we fall back to the current now 
                    sourceAttribute = TransformNode.Attributes[attributeName];
                }
            }
            if (sourceAttribute != null)
            {
                dataValue = sourceAttribute.Value;
            }
            return dataValue;
        }


        //DirRegex treat single quote differently
        protected string EscapeDirRegexSpecialCharacter(string value, bool escape)
        {
            if (escape)
            {
                return value.Replace("'", "&apos;");
            }
            else
            {
                return value.Replace("&apos;", "'");
            }
        }


        protected static string SubstituteKownValue(string transformValue, RegularExpressions.Regex patternRegex, string patternPrefix,  GetValueCallback getValueDelegate )
        {
            int position = 0;
            List<RegularExpressions.Match> matchsExpr = new List<RegularExpressions.Match>();
            do
            {
                position = transformValue.IndexOf(patternPrefix, position, StringComparison.OrdinalIgnoreCase);
                if (position > -1)
                {
                    RegularExpressions.Match match = patternRegex.Match(transformValue, position);
                    // Add the successful match to collection
                    if (match.Success)
                    {
                        matchsExpr.Add(match);
                        position = match.Index + match.Length;
                    }
                    else
                    {
                        position++;
                    }
                }
            } while (position > -1);

            System.Text.StringBuilder strbuilder = new StringBuilder(transformValue.Length);
            if (matchsExpr.Count > 0)
            {
                strbuilder.Remove(0, strbuilder.Length);
                position = 0;
                int index = 0;
                foreach (RegularExpressions.Match match in matchsExpr)
                {
                    strbuilder.Append(transformValue.Substring(position, match.Index - position));
                    RegularExpressions.Capture captureTagName = match.Groups["tagname"];
                    string attributeName = captureTagName.Value;

                    string newValue = getValueDelegate(attributeName);

                    if (newValue != null) // null indicate that the attribute is not exist
                    {
                        strbuilder.Append(newValue);
                    }
                    else
                    {
                        // keep original value
                        strbuilder.Append(match.Value);
                    }
                    position = match.Index + match.Length;
                    index++;
                }
                strbuilder.Append(transformValue.Substring(position));

                transformValue = strbuilder.ToString();
            }

            return transformValue;
        }

        private string GetXPathToAttribute(HtmlAttribute htmlAttribute)
        {
            return GetXPathToAttribute(htmlAttribute, null);
        }

        private string GetXPathToAttribute(HtmlAttribute htmlAttribute, IList<string> locators)
        {
            string path = string.Empty;
            if (htmlAttribute != null)
            {
                string pathToNode = GetXPathToNode(htmlAttribute.OwnerNode);
                if (!string.IsNullOrEmpty(pathToNode))
                {
                    System.Text.StringBuilder identifier = new StringBuilder(256);
                    if (!(locators == null || locators.Count == 0))
                    {
                        foreach (string match in locators)
                        {
                            string val = this.GetAttributeValue(match);
                            if (!string.IsNullOrEmpty(val))
                            {
                                if (identifier.Length != 0)
                                {
                                    identifier.Append(" and ");
                                }
                                identifier.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "@{0}='{1}'", match, val));
                            }
                            else
                            {
                                throw new HtmlTransformationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,Resources.XMLTRANSFORMATION_MatchAttributeDoesNotExist, match));
                            }
                        }
                    }

                    if (identifier.Length == 0) 
                    {
                        for (int i = 0; i < TargetNodes.Count; i++)
                        {
                            if (TargetNodes[i] == htmlAttribute.OwnerNode)
                            {
                                // Xpath is 1 based
                                identifier.Append((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                                break;
                            }
                        }
                    }
                    pathToNode = string.Concat(pathToNode, "[", identifier.ToString(), "]");
                }
                path = string.Concat(pathToNode, "/@", htmlAttribute.Name);
            }
            return path;
        }

        private string GetXPathToNode(HtmlNode htmlNode)
        {
            if (htmlNode == null || htmlNode.NodeType == HtmlNodeType.Document)
            {
                return null;
            }
            string parentPath = GetXPathToNode(htmlNode.ParentNode);
            return string.Concat(parentPath, "/", htmlNode.Name);
        }

        private string TokenizeValue(HtmlAttribute targetAttribute, 
                                     HtmlAttribute transformAttribute, 
                                     bool fTokenizeParameter, 
                                     List<Dictionary<string, string>> parameters)
        {
            Debug.Assert(!fTokenizeParameter || parameters != null);

            tokenizeValueCurrentXmlAttribute = transformAttribute;
            string transformValue = transformAttribute.Value;
            string xpath = GetXPathToAttribute(targetAttribute);

            //subsitute the know value first in the transformAttribute
            transformValue = SubstituteKownValue(transformValue, ParentAttributeRegex, "$(", delegate(string key) { return EscapeDirRegexSpecialCharacter(GetAttributeValue(key), true); });

            // then use the directive to parse the value. --- if TokenizeParameterize is enable
            if (fTokenizeParameter && parameters != null)
            {
                int position = 0;
                System.Text.StringBuilder strbuilder = new StringBuilder(transformValue.Length);
                position = 0;
                List<RegularExpressions.Match> matchs = new List<RegularExpressions.Match>();

                do
                {
                    position = transformValue.IndexOf("{%", position, StringComparison.OrdinalIgnoreCase);
                    if (position > -1)
                    {
                        RegularExpressions.Match match = DirRegex.Match(transformValue, position);
                        // Add the successful match to collection
                        if (match.Success)
                        {
                            matchs.Add(match);
                            position = match.Index + match.Length;
                        }
                        else
                        {
                            position++;
                        }
                    }
                } while (position > -1);

                if (matchs.Count > 0)
                {
                    strbuilder.Remove(0, strbuilder.Length);
                    position = 0;
                    int index = 0;

                    foreach (RegularExpressions.Match match in matchs)
                    {
                        strbuilder.Append(transformValue.Substring(position, match.Index - position));
                        RegularExpressions.CaptureCollection attrnames = match.Groups["attrname"].Captures;
                        if (attrnames != null && attrnames.Count > 0)
                        {
                            RegularExpressions.CaptureCollection attrvalues = match.Groups["attrval"].Captures;
                            Dictionary<string, string> paramDictionary = new Dictionary<string, string>(4, StringComparer.OrdinalIgnoreCase)
                            {
                                [XPathWithIndex] = xpath,
                                [TokenNumber] = index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                            };

                            // Get the key-value pare of the in the tranform form
                            for (int i = 0; i < attrnames.Count; i++)
                            {
                                string name = attrnames[i].Value;
                                string val = null;
                                if (attrvalues != null && i < attrvalues.Count)
                                {
                                    val = EscapeDirRegexSpecialCharacter(attrvalues[i].Value, false);
                                }
                                paramDictionary[name] = val;
                            }

                            //Identify the Token format
                            if (!paramDictionary.TryGetValue(Token, out string strTokenFormat))
                            {
                                strTokenFormat = storageDictionary.TokenFormat;
                            }
                            if (!string.IsNullOrEmpty(strTokenFormat))
                            {
                                paramDictionary[Token] = strTokenFormat;
                            }

                            // Second translation of #() -- replace with the existing Parameters
                            int count = paramDictionary.Count;
                            string[] keys = new string[count];
                            paramDictionary.Keys.CopyTo(keys, 0);
                            for (int i = 0; i < count; i++)
                            {
                                // if token format contain the #(),we replace with the known value such that it is unique identify
                                // for example, intokenizeTransformXml.cs, default token format is
                                // string.Concat("$(ReplacableToken_#(", SetTokenizedAttributes.ParameterAttribute, ")_#(", SetTokenizedAttributes.TokenNumber, "))");
                                // which ParameterAttribute will be translate to parameterDictionary["parameter"} and TokenNumber will be translate to parameter 
                                // parameterDictionary["TokenNumber"]
                                string keyindex = keys[i];
                                string val = paramDictionary[keyindex];
                                string newVal = SubstituteKownValue(val, TokenFormatRegex, "#(",
                                        delegate(string key) { return paramDictionary.ContainsKey(key) ? paramDictionary[key] : null; });

                                paramDictionary[keyindex] = newVal;
                            }

                            if (paramDictionary.TryGetValue(Token, out strTokenFormat))
                            {
                                // Replace with token
                                strbuilder.Append(strTokenFormat);
                            }
                            if (paramDictionary.TryGetValue(XpathLocator, out string attributeLocator) && !string.IsNullOrEmpty(attributeLocator))
                            {
                                IList<string> locators = HtmlArgumentUtility.SplitArguments(attributeLocator);
                                string xpathwithlocator = GetXPathToAttribute(targetAttribute, locators);
                                if (!string.IsNullOrEmpty(xpathwithlocator))
                                {
                                    paramDictionary[XPathWithLocator] = xpathwithlocator;
                                }
                            }
                            parameters.Add(paramDictionary);
                        }

                        position = match.Index + match.Length;
                        index++;
                    }
                    strbuilder.Append(transformValue.Substring(position));
                    transformValue = strbuilder.ToString();
                }
            }
            return transformValue;
        }

    }

    public class RemoveAttributes : RemoveAttributesIfExists
    {
        protected override void ReportTransformFinish()
        {
            if (TargetAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageRemoveAttributes, TargetAttributes.Count);
            }
            else
            {
                Log.LogWarning(TargetNode, Resources.XMLTRANSFORMATION_TransformMessageNoRemoveAttributes);
            }
        }
    }

    public class RemoveAttributesIfExists : AttributeTransform
    {
        protected override void Apply()
        {
            foreach (HtmlAttribute attribute in TargetAttributes)
            {
                TargetNode.Attributes.Remove(attribute);

                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageRemoveAttribute, attribute.Name);
            }

            ReportTransformFinish();
        }

        protected virtual void ReportTransformFinish()
        {
            if (TargetAttributes.Count > 0)
            {
                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageRemoveAttributes, TargetAttributes.Count);
            }
            else
            {
                Log.LogMessage(MessageType.Verbose, Resources.XMLTRANSFORMATION_TransformMessageNoRemoveAttributes);
            }
        }
    }
}
