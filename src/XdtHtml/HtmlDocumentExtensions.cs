using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;

namespace XdtHtml
{
    public static class HtmlDocumentExtensions
    {
        public static HtmlParserOptions ParserDefaultOptions()
        {
            return new HtmlParserOptions
            {
                IsKeepingSourceReferences = true,
                IsPreservingAttributeNames = true,
                IsAcceptingCustomElementsEverywhere = true,
                IsStrictMode = false,
                //IsEmbedded = false,
            };
        }
    }
}
