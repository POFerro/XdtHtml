using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using HtmlAgilityPack;

namespace XdtHtml
{
    public interface IHtmlOriginalDocumentService
    {
        HtmlNodeCollection SelectNodes(string path);
    }
}
