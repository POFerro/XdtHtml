using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using XdtHtml.Properties;
using AngleSharp.Dom;
using AngleSharp.XPath;

namespace XdtHtml
{
    public abstract class AttributeTransform : Transform
    {
        #region private data members
        private IElement transformAttributeSource = null;
        private IList<IAttr> transformAttributes = null;
        private IElement targetAttributeSource = null;
        private IList<IAttr> targetAttributes = null;
        #endregion

        protected new IElement TargetNode => (IElement)base.TargetNode;

        protected AttributeTransform()
            : base(TransformFlags.ApplyTransformToAllTargetNodes) {
        }

        protected IList<IAttr> TransformAttributes {
            get {
                if (transformAttributes == null || transformAttributeSource != TransformNode) {
                    transformAttributeSource = TransformNode;
                    transformAttributes = GetAttributesFrom(TransformNode);
                }
                return transformAttributes;
            }
        }

        protected IList<IAttr> TargetAttributes {
            get {
                if (targetAttributes == null || targetAttributeSource != TargetNode) {
                    targetAttributeSource = TargetNode;
                    targetAttributes = GetAttributesFrom(targetAttributeSource);
                }
                return targetAttributes;
            }
        }

        private IList<IAttr> GetAttributesFrom(IElement node) {
            if (Arguments == null || Arguments.Count == 0) {
                return GetAttributesFrom(node, "*", false);
            }
            else if (Arguments.Count == 1) {
                return GetAttributesFrom(node, Arguments[0], true);
            }
            else {
                // First verify all the arguments
                foreach (string argument in Arguments) {
                    GetAttributesFrom(node, argument, true);
                }

                // Now return the complete XPath and return the combined list
                return GetAttributesFrom(node, Arguments, false);
            }
        }

        private IList<IAttr> GetAttributesFrom(IElement node, string argument, bool warnIfEmpty) {
            return GetAttributesFrom(node, new string[1] { argument }, warnIfEmpty);
        }

        private IList<IAttr> GetAttributesFrom(IElement node, IList<string> arguments, bool warnIfEmpty) {
            string[] array = new string[arguments.Count];
            arguments.CopyTo(array, 0);
            string xpath = String.Concat("@", String.Join("|@", array));

            var attributes = node.SelectNodes(xpath).Cast<IAttr>().ToList();

//            XmlNodeList attributes = node.SelectNodes(xpath);
            if (attributes.Count == 0 && warnIfEmpty) {
                Debug.Assert(arguments.Count == 1, "Should only call warnIfEmpty==true with one argument");
                if (arguments.Count == 1) {
                    Log.LogWarning(Resources.XMLTRANSFORMATION_TransformArgumentFoundNoAttributes, arguments[0]);
                }
            }

            return attributes;
        }
    }
}
