using AngleSharp.Dom;
using System;
using System.Collections.Generic;

namespace XdtHtml
{
    public interface IHtmlOriginalDocumentService
    {
        List<INode> SelectNodes(string path);
    }
}
