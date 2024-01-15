using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using XdtHtml.Properties;
using HtmlAgilityPack;
using System.Linq;

namespace XdtHtml
{
    internal class HtmlElementContext : HtmlNodeContext
    {
        #region private data members
        private readonly HtmlElementContext parentContext;
        private string xpath = null;
        private string parentXPath = null;
        private readonly HtmlDocument htmlTargetDoc;

        private readonly IServiceProvider serviceProvider;

        private HtmlNode transformNode = null;
        private List<HtmlNode> transformNodePreamble = null;
        private List<HtmlNode> transformNodePostamble = null;

        private HtmlNodeCollection targetNodes = null;
        private HtmlNodeCollection targetParents = null;

        private HtmlAttribute transformAttribute = null;
        private HtmlAttribute locatorAttribute = null;

        private readonly string xdtPrefix;

        //private HtmlNamespaceManager namespaceManager = null;
        #endregion

        public HtmlElementContext(HtmlElementContext parent, HtmlNode element, HtmlDocument htmlTargetDoc, IServiceProvider serviceProvider, string xdtPrefix)
            : base(element) {
            this.parentContext = parent;
            this.htmlTargetDoc = htmlTargetDoc;
            this.serviceProvider = serviceProvider;
            this.xdtPrefix = xdtPrefix;
        }

        public T GetService<T>() where T : class {
            if (serviceProvider != null) {
                T service = serviceProvider.GetService(typeof(T)) as T;
                // now it is legal to return service that's null -- due to SetTokenizeAttributeStorage
                //Debug.Assert(service != null, String.Format(CultureInfo.InvariantCulture, "Service provider didn't provide {0}", typeof(ServiceType).Name));
                return service;
            }
            else {
                Debug.Fail("No ServiceProvider");
                return null;
            }
        }

        #region data accessors
        public HtmlNode Element {
            get {
                return Node;
            }
        }

        public string XPath {
            get {
                if (xpath == null) {
                    xpath = ConstructXPath();
                }
                return xpath;
            }
        }

        public string ParentXPath {
            get {
                if (parentXPath == null) {
                    parentXPath = ConstructParentXPath();
                }
                return parentXPath;
            }
        }

        public Transform ConstructTransform(out string argumentString) {
            try {
                return CreateObjectFromAttribute<Transform>(out argumentString, out transformAttribute);
            }
            catch (Exception ex) {
                throw WrapException(ex);
            }
        }

        public int TransformLineNumber {
            get {
                //IXmlLineInfo lineInfo = transformAttribute as IXmlLineInfo;
                //if (lineInfo != null) {
                    return transformAttribute.Line;
                //}
                //else {
                //    return LineNumber;
                //}
            }
        }

        public int TransformLinePosition {
            get {
                //IXmlLineInfo lineInfo = transformAttribute as IXmlLineInfo;
                //if (lineInfo != null) {
                    return transformAttribute.LinePosition;
                //}
                //else {
                //    return LinePosition;
                //}
            }
        }

        public HtmlAttribute TransformAttribute {
            get {
                return transformAttribute;
            }
        }

        public HtmlAttribute LocatorAttribute {
            get {
                return locatorAttribute;
            }
        }
        #endregion

        #region XPath construction
        private string ConstructXPath() {
            try {
                string argumentString;
                string parentPath = parentContext == null ? String.Empty : parentContext.XPath;

                Locator locator = CreateLocator(out argumentString);

                return locator.ConstructPath(parentPath, this, argumentString);
            }
            catch (Exception ex) {
                throw WrapException(ex);
            }
        }

        private string ConstructParentXPath() {
            try {
                string argumentString;
                string parentPath = parentContext == null ? String.Empty : parentContext.XPath;

                Locator locator = CreateLocator(out argumentString);

                return locator.ConstructParentPath(parentPath, this, argumentString);
            }
            catch (Exception ex) {
                throw WrapException(ex);
            }
        }

        private Locator CreateLocator(out string argumentString) {
            Locator locator = CreateObjectFromAttribute<Locator>(out argumentString, out locatorAttribute);
            if (locator == null) {
                argumentString = null;
                //avoid using singleton of "DefaultLocator.Instance", so unit tests can run parallel
                locator = new DefaultLocator();
            }
            return locator;
        }
        #endregion

        #region Context information
        internal HtmlNode TransformNode {
            get {
                if (transformNode == null) {
                    transformNode = CreateCloneInTargetDocument(Element);
                }
                return transformNode;
            }
        }
        internal IEnumerable<HtmlNode> TransformNodePreamble
        {
            get
            {
                if (transformNodePreamble == null)
                {
                    transformNodePreamble = Element
                        .GetNodePreamble()
                        .Reverse()
                        .TakeWhile(n => n.NodeType == HtmlNodeType.Text)
                        .Reverse()
                        .Select(e => CreateCloneInTargetDocument(e))
                        .ToList();
                }
                return transformNodePreamble;
            }
        }
        internal IEnumerable<HtmlNode> TransformNodePostamble
        {
            get
            {
                if (transformNodePostamble == null)
                {
                    transformNodePostamble = Element
                        .GetNodePostamble()
                        .TakeWhile(n => n.NodeType == HtmlNodeType.Text)
                        .OfType<HtmlTextNode>()
                        .Select(e => CreateCloneInTargetDocument(e))
                        .ToList();
                }
                return transformNodePostamble;
            }
        }

        internal HtmlNodeCollection TargetNodes {
            get {
                if (targetNodes == null) {
                    targetNodes = GetTargetNodes(XPath);
                }
                return targetNodes;
            }
        }

        internal HtmlNodeCollection TargetParents {
            get {
                if (targetParents == null && parentContext != null) {
                    targetParents = GetTargetNodes(ParentXPath);
                }
                return targetParents;
            }
        }
        #endregion

        #region Node helpers
        private HtmlDocument TargetDocument {
            get {
                return htmlTargetDoc;
            }
        }

        private HtmlNode CreateCloneInTargetDocument(HtmlNode sourceNode) {
            
            HtmlNode clonedNode = HtmlNode.CreateNode(sourceNode.OuterHtml, (doc) => {
                doc.WithDefaultOptions();
            });

            ScrubTransformAttributesAndNamespaces(clonedNode);

            return clonedNode;
        }

        private void ScrubTransformAttributesAndNamespaces(HtmlNode node) {
            if (node.Attributes != null) {
                List<HtmlAttribute> attributesToRemove = new List<HtmlAttribute>();
                foreach (HtmlAttribute attribute in node.Attributes) {
                    if (attribute.Name.StartsWith(this.xdtPrefix + ":")) {
                        attributesToRemove.Add(attribute);
                    }
                }
                foreach (HtmlAttribute attributeToRemove in attributesToRemove) {
                    node.Attributes.Remove(attributeToRemove);
                }
            }

            // Do the same recursively for child nodes
            foreach (HtmlNode childNode in node.ChildNodes) {
                ScrubTransformAttributesAndNamespaces(childNode);
            }
        }

        private HtmlNodeCollection GetTargetNodes(string xpath) {
            return TargetDocument.DocumentNode.SelectNodes(xpath);
        }

        private Exception WrapException(Exception ex) {
            return HtmlNodeException.Wrap(ex, Element);
        }

        private Exception WrapException(Exception ex, HtmlNode node) {
            return HtmlNodeException.Wrap(ex, node);
        }

        private Exception WrapException(Exception ex, HtmlAttribute attribute)
        {
            return HtmlNodeException.Wrap(ex, attribute);
        }

        //private XmlNamespaceManager GetNamespaceManager() {
        //    if (namespaceManager == null) {
        //        XmlNodeList localNamespaces = Element.SelectNodes("namespace::*");

        //        if (localNamespaces.Count > 0) {
        //            namespaceManager = new XmlNamespaceManager(Element.OwnerDocument.NameTable);

        //            foreach (XmlAttribute nsAttribute in localNamespaces) {
        //                string prefix = String.Empty;
        //                int index = nsAttribute.Name.IndexOf(':');
        //                if (index >= 0) {
        //                    prefix = nsAttribute.Name.Substring(index + 1);
        //                }
        //                else {
        //                    prefix = "_defaultNamespace";
        //                }

        //                namespaceManager.AddNamespace(prefix, nsAttribute.Value);
        //            }
        //        }
        //        else {
        //            namespaceManager = new XmlNamespaceManager(GetParentNameTable());
        //        }
        //    }
        //    return namespaceManager;
        //}

        //private XmlNameTable GetParentNameTable() {
        //    if (parentContext == null) {
        //        return Element.OwnerDocument.DocumentNode.NameTable;
        //    }
        //    else {
        //        return parentContext.GetNamespaceManager().NameTable;
        //    }
        //}
        #endregion

        #region Named object creation
        private static Regex nameAndArgumentsRegex = null;
        private Regex NameAndArgumentsRegex {
            get {
                if (nameAndArgumentsRegex == null) {
                    nameAndArgumentsRegex = new Regex(@"\A\s*(?<name>\w+)(\s*\((?<arguments>.*)\))?\s*\Z", RegexOptions.Compiled|RegexOptions.Singleline);
                }
                return nameAndArgumentsRegex;
            }
        }

        private string ParseNameAndArguments(string name, out string arguments) {
            arguments = null;

            System.Text.RegularExpressions.Match match = NameAndArgumentsRegex.Match(name);
            if (match.Success) {
                if (match.Groups["arguments"].Success) {
                    CaptureCollection argumentCaptures = match.Groups["arguments"].Captures;
                    if (argumentCaptures.Count == 1 && !String.IsNullOrEmpty(argumentCaptures[0].Value)) {
                        arguments = argumentCaptures[0].Value;
                    }
                }

                return match.Groups["name"].Captures[0].Value;
            }
            else {
                throw new HtmlTransformationException(Resources.XMLTRANSFORMATION_BadAttributeValue);
            }
        }

        private ObjectType CreateObjectFromAttribute<ObjectType>(out string argumentString, out HtmlAttribute objectAttribute) where ObjectType : class {
            objectAttribute = Element.Attributes[this.xdtPrefix + ":" + typeof(ObjectType).Name];
            try {
                if (objectAttribute != null) {
                    string typeName = ParseNameAndArguments(objectAttribute.DeEntitizeValue, out argumentString);
                    if (!String.IsNullOrEmpty(typeName)) {
                        NamedTypeFactory factory = GetService<NamedTypeFactory>();
                        return factory.Construct<ObjectType>(typeName);
                    }
                }
            }
            catch (Exception ex) {
                throw WrapException(ex, objectAttribute);
            }

            argumentString = null;
            return null;
        }
        #endregion

        #region Error reporting helpers
        internal bool HasTargetNode(out HtmlElementContext failedContext, out bool existedInOriginal) {
            failedContext = null;
            existedInOriginal = false;

            if (TargetNodes.Count == 0) {
                failedContext = this;
                while (failedContext.parentContext != null &&
                    failedContext.parentContext.TargetNodes.Count == 0) {

                    failedContext = failedContext.parentContext;
                }

                existedInOriginal = ExistedInOriginal(failedContext.XPath);
                return false;
            }

            return true;
        }

        internal bool HasTargetParent(out HtmlElementContext failedContext, out bool existedInOriginal) {
            failedContext = null;
            existedInOriginal = false;

            if (TargetParents.Count == 0) {
                failedContext = this;
                while (failedContext.parentContext != null &&
                    !String.IsNullOrEmpty(failedContext.parentContext.ParentXPath) &&
                    failedContext.parentContext.TargetParents.Count == 0) {

                    failedContext = failedContext.parentContext;
                }

                existedInOriginal = ExistedInOriginal(failedContext.XPath);
                return false;
            }

            return true;
        }

        private bool ExistedInOriginal(string xpath) {
            IHtmlOriginalDocumentService service = GetService<IHtmlOriginalDocumentService>();
            if (service != null) {
                HtmlNodeCollection nodeList = service.SelectNodes(xpath);
                if (nodeList != null && nodeList.Count > 0) {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
