using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace XdtHtml
{
    public static class HtmlDocumentExtensions
    {
        public static HtmlDocument WithDefaultOptions(this HtmlDocument newDoc)
        {
            newDoc.OptionOutputOriginalCase = true;
            newDoc.OptionUseIdAttribute = false;
            newDoc.OptionPreserveXmlNamespaces = true;
            newDoc.OptionCheckSyntax = false;
            newDoc.OptionWriteEmptyNodes = true;
            newDoc.OptionEmptyCollection = true;
            newDoc.OptionDefaultUseOriginalName = true;

            return newDoc;
        }
    }
}
