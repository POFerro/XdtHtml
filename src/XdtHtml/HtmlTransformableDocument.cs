using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using HtmlAgilityPack;

namespace XdtHtml
{
    public class HtmlTransformableDocument : HtmlDocument, IHtmlOriginalDocumentService
    {
        #region private data members
        private HtmlDocument htmlOriginal = null;
        #endregion

        #region public interface
        public HtmlTransformableDocument() {
            this.WithDefaultOptions();
        }

        public bool IsChanged {
            get {
                if (htmlOriginal == null) {
                    // No transformation has occurred
                    return false;
                }

                return !IsHtmlEqual(htmlOriginal, this);
            }
        }
        #endregion

        #region Change support
        internal void OnBeforeChange() {
            if (htmlOriginal == null) {
                CloneOriginalDocument();
            }
        }

        internal void OnAfterChange() {
        }
        #endregion

        #region Helper methods
        private void CloneOriginalDocument() {
            htmlOriginal = new HtmlDocument().WithDefaultOptions();
            htmlOriginal.LoadHtml(this.DocumentNode.OuterHtml);
        }

        private bool IsHtmlEqual(HtmlDocument htmlOriginal, HtmlDocument htmlTransformed) {
            // FUTURE: Write a comparison algorithm to see if htmlLeft and
            // htmlRight are different in any significant way. Until then,
            // assume there's a difference.
            return false;
        }
        #endregion

        #region IXmlOriginalDocumentService Members
        HtmlNodeCollection IHtmlOriginalDocumentService.SelectNodes(string xpath) {
            if (htmlOriginal != null) {
                return htmlOriginal.DocumentNode.SelectNodes(xpath);
            }
            else {
                return null;
            }
        }
        #endregion
    }
}
