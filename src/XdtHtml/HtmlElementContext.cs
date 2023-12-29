using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using XdtHtml.Properties;
using AngleSharp.Dom;
using AngleSharp.XPath;
using System.Linq;

namespace XdtHtml
{
    internal class HtmlElementContext : HtmlNodeContext
    {
        #region private data members
        private readonly HtmlElementContext parentContext;
        private string xpath = null;
        private string parentXPath = null;
        private readonly IDocument htmlTargetDoc;

        private readonly IServiceProvider serviceProvider;

        private IElement transformNodes = null;
        private List<INode> targetNodes = null;
        private List<INode> targetParents = null;

        private IAttr transformAttribute = null;
        private IAttr locatorAttribute = null;

        //private HtmlNamespaceManager namespaceManager = null;
        #endregion

        public HtmlElementContext(HtmlElementContext parent, IElement element, IDocument htmlTargetDoc, IServiceProvider serviceProvider)
            : base(element) {
            this.parentContext = parent;
            this.htmlTargetDoc = htmlTargetDoc;
            this.serviceProvider = serviceProvider;
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
        public IElement Element {
            get {
                return (IElement)Node;
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
                if (transformAttribute.OwnerElement?.SourceReference != null)
                {
                    return transformAttribute.OwnerElement.SourceReference.Position.Line;
                }
                else
                {
                    return LineNumber;
                }
            }
        }

        public int TransformLinePosition {
            get {
                if (transformAttribute.OwnerElement?.SourceReference != null)
                {
                    return transformAttribute.OwnerElement.SourceReference.Position.Column;
                }
                else
                {
                    return LinePosition;
                }
            }
        }

        public IAttr TransformAttribute {
            get {
                return transformAttribute;
            }
        }

        public IAttr LocatorAttribute {
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
        internal IElement TransformNode {
            get {
                if (transformNodes == null) {
                    transformNodes = CreateCloneInTargetDocument(Element);
                }
                return transformNodes;
            }
        }

        internal List<INode> TargetNodes {
            get {
                if (targetNodes == null) {
                    targetNodes = GetTargetNodes(XPath);
                }
                return targetNodes;
            }
        }

        internal List<INode> TargetParents {
            get {
                if (targetParents == null && parentContext != null) {
                    targetParents = GetTargetNodes(ParentXPath);
                }
                return targetParents;
            }
        }
        #endregion

        #region Node helpers
        private IDocument TargetDocument {
            get {
                return htmlTargetDoc;
            }
        }

        private IElement CreateCloneInTargetDocument(IElement sourceNode) {
            
            IElement clonedNode = (IElement)htmlTargetDoc.Import(sourceNode);

            ScrubTransformAttributesAndNamespaces(clonedNode);

            return clonedNode;
        }

        private void ScrubTransformAttributesAndNamespaces(IElement node) {
            if (node.Attributes != null) {
                foreach (IAttr attributeToRemove in node.Attributes.Where(a => a.NamespaceUri == HtmlTransformation.TransformNamespace)) {
                    attributeToRemove.RemoveFromParent();
                }
            }

            // Do the same recursively for child nodes
            foreach (IElement childNode in node.Children) {
                ScrubTransformAttributesAndNamespaces(childNode);
            }
        }

        private List<INode> GetTargetNodes(string xpath) {
            return TargetDocument.DocumentElement.SelectNodes(xpath);
        }

        private Exception WrapException(Exception ex) {
            return HtmlNodeException.Wrap(ex, Element);
        }

        private Exception WrapException(Exception ex, INode node) {
            return HtmlNodeException.Wrap(ex, node);
        }

        private Exception WrapException(Exception ex, Attr attribute)
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

        private ObjectType CreateObjectFromAttribute<ObjectType>(out string argumentString, out IAttr objectAttribute) where ObjectType : class {
            objectAttribute = Element.Attributes.FirstOrDefault(a => a.NamespaceUri == HtmlTransformation.TransformNamespace && a.LocalName == typeof(ObjectType).Name);
            try {
                if (objectAttribute != null) {
                    string typeName = ParseNameAndArguments(objectAttribute.Value, out argumentString);
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
                List<INode> nodeList = service.SelectNodes(xpath);
                if (nodeList != null && nodeList.Count > 0) {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
