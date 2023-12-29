using AngleSharp.Dom;
using System;
using System.Collections.Generic;

namespace XdtHtml
{
    internal class HtmlNodeContext
    {
        #region private data members
        private INode node;
        #endregion

        public HtmlNodeContext(INode node) {
            this.node = node;
        }

        #region data accessors
        public INode Node {
            get {
                return node;
            }
        }

        public bool HasLineInfo {
            get {
                return (node as IElement)?.SourceReference != null;
            }
        }

        public int LineNumber {
            get {
                return (node as IElement)?.SourceReference?.Position.Line ?? 0;
            }
        }

        public int LinePosition {
            get {
                return (node as IElement)?.SourceReference?.Position.Column ?? 0;
            }
        }
        #endregion
    }
}
