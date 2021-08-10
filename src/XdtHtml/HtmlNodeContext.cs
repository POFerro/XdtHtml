using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;

namespace XdtHtml
{
    internal class HtmlNodeContext
    {
        #region private data members
        private HtmlNode node;
        #endregion

        public HtmlNodeContext(HtmlNode node) {
            this.node = node;
        }

        #region data accessors
        public HtmlNode Node {
            get {
                return node;
            }
        }

        public bool HasLineInfo {
            get {
                return true; // node is IXmlLineInfo;
            }
        }

        public int LineNumber {
            get {
                //IXmlLineInfo lineInfo = node as IXmlLineInfo;
                //if (lineInfo != null) {
                    return node.Line;
                //}
                //else {
                //    return 0;
                //}
            }
        }

        public int LinePosition {
            get {
                //IXmlLineInfo lineInfo = node as IXmlLineInfo;
                //if (lineInfo != null) {
                    return node.LinePosition;
                //}
                //else {
                //    return 0;
                //}
            }
        }
        #endregion
    }
}
