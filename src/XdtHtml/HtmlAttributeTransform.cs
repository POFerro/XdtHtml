using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;
using XdtHtml.Properties;
using HtmlAgilityPack;

namespace XdtHtml
{
    public abstract class AttributeTransform : Transform
    {
        #region private data members
        private HtmlNode lastTransformAttributeSource = null;
        private IList<HtmlAttribute> transformAttributes = null;
        private HtmlNode targetAttributeSource = null;
        private IList<HtmlAttribute> targetAttributes = null;
        #endregion

        protected AttributeTransform()
            : base(TransformFlags.ApplyTransformToAllTargetNodes) {
        }

        protected virtual HtmlNode TransformAttributeSource => TransformNode;
        protected virtual IList<string> AttributeNamesArguments => this.Arguments;

        private HtmlNode lastEvaluatedAttributesNode = null;
        private IList<string> lastEvaluatedAttributeNames = null;
        protected virtual IList<string> GetAttributeNamesArgument(HtmlNode node) 
        {
            if (lastEvaluatedAttributeNames == null || lastEvaluatedAttributesNode != node)
            {
                lastEvaluatedAttributesNode = node;
                var attrNames = this.AttributeNamesArguments;
                if (attrNames == null || attrNames.Count == 0)
                {
                    lastEvaluatedAttributeNames = new [] { "*" };
                }
                else if (attrNames.Count == 1)
                {
                    lastEvaluatedAttributeNames = new[] { attrNames[0] };
                }
                else
                {
                    // First verify all the arguments
                    foreach (string argument in attrNames)
                    {
                        GetAttributesFrom(node, new string[1] { argument }, true);
                    }

                    // Now return the complete XPath and return the combined list
                    lastEvaluatedAttributeNames = attrNames;
                }
            }
            return lastEvaluatedAttributeNames;
        }

        protected IList<HtmlAttribute> TransformAttributes {
            get {
                if (transformAttributes == null || lastTransformAttributeSource != TransformAttributeSource) {
                    lastTransformAttributeSource = TransformAttributeSource;
                    transformAttributes = GetAttributesFrom(lastTransformAttributeSource);
                }
                return transformAttributes;
            }
        }

        protected IList<HtmlAttribute> TargetAttributes {
            get {
                if (targetAttributes == null || targetAttributeSource != TargetNode) {
                    targetAttributeSource = TargetNode;
                    targetAttributes = GetAttributesFrom(TargetNode);
                }
                return targetAttributes;
            }
        }

        private IList<HtmlAttribute> GetAttributesFrom(HtmlNode node) {
            return GetAttributesFrom(node, GetAttributeNamesArgument(node), Arguments?.Count == 1);
        }

        private IList<HtmlAttribute> GetAttributesFrom(HtmlNode node, IList<string> arguments, bool warnIfEmpty) {
            string[] array = new string[arguments.Count];
            arguments.CopyTo(array, 0);
            string xpath = String.Concat("@", String.Join("|@", array));

            var attributeNames = node.CreateNavigator().Select(xpath).Cast<HtmlNodeNavigator>().Select(n => n.LocalName).ToArray();
            var attributes = node.GetAttributes(attributeNames).ToList();

//            XmlNodeList attributes = node.SelectNodes(xpath);
            if (attributes.Count == 0/* && warnIfEmpty*/) {
                //Debug.Assert(arguments.Count == 1, "Should only call warnIfEmpty==true with one argument");
                //if (arguments.Count == 1) {
                    Log.LogWarning(Resources.XMLTRANSFORMATION_TransformArgumentFoundNoAttributes, xpath);
                //}
            }

            return attributes;
        }
    }
}
