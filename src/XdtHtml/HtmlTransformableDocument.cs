using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using System;
using System.Collections.Generic;
using System.IO;

namespace XdtHtml
{
    public class HtmlTransformableDocument : IHtmlOriginalDocumentService
    {
        #region private data members
        private IDocument originalDoc = null;
        private IDocument innerDocument = null;
        #endregion

        [Obsolete("This property is obsolete, please use " + nameof(DocumentElement))]
        public IElement DocumentNode => this.DocumentElement;

        public IElement DocumentElement => this.innerDocument?.DocumentElement;
        public IDocument Document => this.innerDocument;

        #region public interface
        public HtmlTransformableDocument() {
        }

        public void LoadHtml(string html)
        {
            var parser = new HtmlParser(HtmlDocumentExtensions.ParserDefaultOptions());
            innerDocument = parser.ParseDocument(html);
        }
        public void Load(string path)
        {
            var parser = new HtmlParser(HtmlDocumentExtensions.ParserDefaultOptions());
            innerDocument = BrowsingContext.NewFrom(parser).OpenAsync(r => r.Address(path)).Result;
        }
        public void Load(Stream file)
        {
            var parser = new HtmlParser(HtmlDocumentExtensions.ParserDefaultOptions());
            innerDocument = BrowsingContext.NewFrom(parser).OpenAsync(r => r.Content(file)).Result;
            //innerDocument = parser.ParseDocument(file);
        }

        public bool IsChanged {
            get {
                if (originalDoc == null) {
                    // No transformation has occurred
                    return false;
                }

                return !IsHtmlEqual(originalDoc, this.innerDocument);
            }
        }
        #endregion

        #region Change support
        internal void OnBeforeChange() {
            if (originalDoc == null) {
                CloneOriginalDocument();
            }
        }

        internal void OnAfterChange() {
        }
        #endregion

        #region Helper methods
        private void CloneOriginalDocument() {
            originalDoc = (IDocument)this.innerDocument.Clone(true);
        }

        private bool IsHtmlEqual(IDocument htmlOriginal, IDocument htmlTransformed) {
            // FUTURE: Write a comparison algorithm to see if htmlLeft and
            // htmlRight are different in any significant way. Until then,
            // assume there's a difference.
            return false;
        }
        #endregion

        #region IXmlOriginalDocumentService Members
        List<INode> IHtmlOriginalDocumentService.SelectNodes(string xpath) {
            if (originalDoc != null) {
                return originalDoc.DocumentElement.SelectNodes(xpath);
            }
            else {
                return null;
            }
        }

        public string ToHtml()
        {
            return this.DocumentElement.Prettify();
        }
        #endregion
    }
}
