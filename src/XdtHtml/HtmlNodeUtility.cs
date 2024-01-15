using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace XdtHtml
{
    public static class HtmlNodeUtility
    {
        public static IEnumerable<HtmlNode> GetPreviousSiblings(this HtmlNode node, Func<HtmlNode, bool> breakBefore = null, Func<HtmlNode, bool> breakAfter = null)
        {
            var sibling = node.PreviousSibling;
            if (sibling != null && breakBefore?.Invoke(sibling) != true)
            {
                yield return sibling;

                foreach (var previousSibling in sibling.GetPreviousSiblings(breakBefore, breakAfter))
                {
                    yield return previousSibling;

                    if (breakAfter?.Invoke(previousSibling) == true)
                        yield break;
                }
            }
        }

        public static IEnumerable<HtmlNode> GetNextSiblings(this HtmlNode node, Func<HtmlNode, bool> breakBefore = null, Func<HtmlNode, bool> breakAfter = null)
        {
            var sibling = node.NextSibling;
            if (sibling != null && breakBefore?.Invoke(sibling) != true)
            {
                yield return sibling;

                foreach (var nextSibling in sibling.GetNextSiblings(breakBefore, breakAfter))
                {
                    yield return nextSibling;

                    if (breakAfter?.Invoke(nextSibling) == true)
                        yield break;
                }
            }
        }

        public static HtmlNode GetFirstChildElement(this HtmlNode node)
        {
            return node.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlNodeType.Element);
        }

        public static IEnumerable<HtmlNode> GetNodePreamble(this HtmlNode node)
        {
            return node.GetPreviousSiblings(breakBefore: n => n.NodeType != HtmlNodeType.Text && n.NodeType != HtmlNodeType.Comment).Reverse();
        }

        public static IEnumerable<HtmlNode> GetNodePostamble(this HtmlNode node)
        {
            return node.GetNextSiblings(breakBefore: n => n.NodeType != HtmlNodeType.Text && n.NodeType != HtmlNodeType.Comment);
        }

        public static IEnumerable<HtmlNode> GetNodeWithPreamble(this HtmlNode node)
        {
            foreach (var preamble in node.GetNodePreamble())
            {
                yield return preamble;
            }
            yield return node;
        }

        public static IEnumerable<HtmlNode> GetEndTagPreamble(this HtmlNode node)
        {
            var lastChild = node.LastChild;
            if (lastChild != null && (lastChild.NodeType == HtmlNodeType.Text || lastChild.NodeType == HtmlNodeType.Comment))
            {
                return lastChild.GetNodeWithPreamble();
            }

            return Enumerable.Empty<HtmlNode>();
        }

        public static IEnumerable<HtmlNode> GetNodeWithPostamble(this HtmlNode node)
        {
            yield return node;

            foreach (var preamble in node.GetNodePostamble())
            {
                yield return preamble;
            }
        }

        public static HtmlNode AdjustIndent(this HtmlNode node, HtmlNode indentReference)
        {
            var desiredIndent = indentReference.GetIndentationWithNewline();

            return AdjustIndent(node, desiredIndent, node.GetIndentationWithNewline());
        }

        public static IEnumerable<HtmlNode> AdjustIndent(this IEnumerable<HtmlNode> list, HtmlNode indentReference)
        {
            var desiredIndent = indentReference.GetIndentationWithNewline();

            return list.AdjustIndent(desiredIndent);
        }
        public static IEnumerable<HtmlNode> AdjustIndent(this IEnumerable<HtmlNode> list, string desiredIndent)
        {
            foreach (var node in list)
            {
                yield return node.AdjustIndent(desiredIndent, node.GetIndentationWithNewline());
            }
        }
        public static IEnumerable<HtmlNode> AdjustIndent(this IEnumerable<HtmlNode> list, string desiredIndent, string currentElementsIndent)
        {
            foreach (var node in list)
            {
                yield return node.AdjustIndent(desiredIndent, currentElementsIndent);
            }
        }

        public static HtmlNode AdjustParentIndent(this HtmlNode node, HtmlNode indentParentReference)
        {
            return node.AdjustParentIndent(indentParentReference.GetIndentationWithNewline(), node.GetIndentationWithNewline(), node.GetIndentationFromParent());
        }
        //public static HtmlNode AdjustParentIndent(this HtmlNode node, string desiredParentIndent)
        //{
        //    return AdjustParentIndent(node, desiredParentIndent, node.GetIndentationWithNewline(), node.GetIndentationFromParent());
        //}
        public static HtmlNode AdjustParentIndent(this HtmlNode node, string desiredParentIndent, string currentElementIndent, string currentElementParentIndent)
        {
            var desiredIndentation = desiredParentIndent + currentElementParentIndent;
            return AdjustIndent(node, desiredIndentation, currentElementIndent);
        }

        public static IEnumerable<HtmlNode> AdjustParentIndent(this IEnumerable<HtmlNode> list, HtmlNode indentParentReference)
        {
            var desiredParentIndent = indentParentReference.GetIndentationWithNewline();
            foreach (var node in list)
            {
                yield return node.AdjustParentIndent(desiredParentIndent, node.GetIndentationWithNewline(), node.GetIndentationFromParent());
            }
        }
        //public static IEnumerable<HtmlNode> AdjustParentIndent(this IEnumerable<HtmlNode> list, string desiredParentIndent)
        //{
        //    foreach (var node in list)
        //    {
        //        yield return node.AdjustParentIndent(desiredParentIndent);
        //    }
        //}
        public static IEnumerable<HtmlNode> AdjustParentIndent(this IEnumerable<HtmlNode> list, string desiredParentIndent, string currentElementIndent, string currentElementParentIndent)
        {
            foreach (var node in list)
            {
                yield return node.AdjustParentIndent(desiredParentIndent, currentElementIndent, currentElementParentIndent);
            }
        }

        public static HtmlNode AdjustIndent(this HtmlNode node, string desiredIndent, string currentElementIndent)
        {
            var identInfo = GetIndentTreeInfo(node, desiredIndent, currentElementIndent);
            AdjustIndent(identInfo);

            //var nodeIndentation = node.GetIndentationWithNewline();

            //var childrenIndent = node.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element).Select(n => n.GetIndentationFromParent()).FirstOrDefault();

            //var indentTreeInfo = GetIndentTreeInfo(node, previousIndent);

            //if (node is HtmlTextNode text && nodeIndentation.Length > 0)
            //{
            //    text.Text = text.Text.Replace(nodeIndentation, previousIndent);
            //}
            //foreach (var child in node.ChildNodes)
            //{
            //    AdjustIndent(child, previousIndent + (childrenIndent ?? ""));
            //}

            return node;
        }

        private static void AdjustIndent(IndentTreeInfo node)
        {
            if (node.PreambleNode != null)
            {
                node.PreambleNode.Text = node.PreambleNode.Text.Replace(node.NodeIndentation, node.DesiredIndentation);
            }
            if (node.EndTagPreamble != null && node.EndTagIndentation.Length > 0)
            {
                node.EndTagPreamble.Text = node.EndTagPreamble.Text.Replace(node.EndTagIndentation, node.DesiredIndentation);
            }
            foreach (var child in node.ChildNodes)
            {
                AdjustIndent(child);
            }
        }


        private class IndentTreeInfo
        {
            public string NodeIndentation { get; set; }
            public string DesiredIndentation { get; set; }

            //public IEnumerable<HtmlTextNode> IndentationNodes { get; set; }

            public IndentTreeInfo ParentNode { get; set; }
            public IEnumerable<IndentTreeInfo> ChildNodes { get; set; }
            public HtmlNode CurrentNode { get; internal set; }
            public HtmlTextNode PreambleNode { get; internal set; }
            public HtmlTextNode EndTagPreamble { get; internal set; }
            public string EndTagIndentation { get; set; }
        }

        private static IndentTreeInfo GetIndentTreeInfo(HtmlNode node, string desiredIndent, string currentNodeIndentation)
        {
            var preamble = node.GetNodeWithPreamble()
                               .OfType<HtmlTextNode>()
                               .LastOrDefault();
            HtmlTextNode postamble = null;
            if (node.NodeType == HtmlNodeType.Element)
            {
                postamble = node.GetEndTagPreamble()
                                .OfType<HtmlTextNode>()
                                .LastOrDefault();
            }
            var info = new IndentTreeInfo
            {
                NodeIndentation = currentNodeIndentation,
                DesiredIndentation = desiredIndent,
                CurrentNode = node,
                
                PreambleNode = preamble,
                EndTagPreamble = postamble,
                EndTagIndentation = postamble?.GetIndentationWithNewline()
            };

            info.ChildNodes = node.ChildNodes
                                 .Where(n => n.NodeType == HtmlNodeType.Element || n.NodeType == HtmlNodeType.Comment)
                                 .Select(n => GetIndentTreeInfo(n, desiredIndent + n.GetIndentationFromParent(currentNodeIndentation), n.GetIndentationWithNewline()))
                                 .ToList()
                                 ;

            return info;
        }

        public static string GetIndentationFromParent(this HtmlNode node, string parentNodeIndentation)
        {
            if (node.ParentNode == null || node.ParentNode.NodeType == HtmlNodeType.Text)
                return node.GetIndentationWithNewline().Replace("\r", "").Replace("\n", "");
            else
            {
                var nodeIndentation = node.GetIndentationWithNewline();
                if (string.IsNullOrEmpty(parentNodeIndentation) || !nodeIndentation.Contains(parentNodeIndentation))
                    return nodeIndentation.Replace("\r", "").Replace("\n", "");
                else
                    return nodeIndentation.Replace(parentNodeIndentation, "");
            }
        }

        public static string GetIndentationFromParent(this HtmlNode node)
        {
            if (node.ParentNode == null || node.ParentNode.NodeType == HtmlNodeType.Text)
                return node.GetIndentationWithNewline().Replace("\r", "").Replace("\n", "");
            else
            {
                var parentNodeIndentation = node.ParentNode.GetIndentationWithNewline();
                var nodeIndentation = node.GetIndentationWithNewline();
                if (string.IsNullOrEmpty(parentNodeIndentation) || !nodeIndentation.Contains(parentNodeIndentation))
                    return nodeIndentation.Replace("\r", "").Replace("\n", "");
                else
                    return nodeIndentation.Replace(parentNodeIndentation, "");
            }
        }

        public static string GetIndentationWithNewline(this HtmlNode node)
        {
            var lastIndentText = node.GetNodeWithPreamble()
                       .OfType<HtmlTextNode>()
                       .Where(n => n.Text.Contains("\n"))
                       .LastOrDefault()?.Text;

            if (lastIndentText == null)
                return "";

            var lastIndentIndex = lastIndentText.LastIndexOf("\r");
            if (lastIndentIndex == -1)
                lastIndentIndex = lastIndentText.LastIndexOf("\n");
            return lastIndentText.Substring(lastIndentIndex);
                           //.Split('\n')
                           //.Select(n => n + '\n')
                           //.First();
            //return node.GetNodeWithPreamble()
            //           .OfType<HtmlTextNode>()
            //           .Where(n => n.Text.Contains("\n"))
            //           .LastOrDefault()?
            //               .Text.Split('\n')
            //               .Select(n => n + '\n')
            //               .First();
        }

        public static IEnumerable<HtmlNode> AppendChild(this HtmlNode parentNode, IEnumerable<HtmlNode> newChildren)
        {
            return newChildren.Select(child => parentNode.AppendChild(child)).ToList();
        }

        public static IEnumerable<HtmlNode> PrependChild(this HtmlNode parentNode, IEnumerable<HtmlNode> newChildren)
        {
            return newChildren.Reverse().Select(child => parentNode.PrependChild(child)).Reverse().ToList();
        }

        public static IEnumerable<HtmlNode> InsertBefore(this HtmlNode parentNode, IEnumerable<HtmlNode> newChildren, HtmlNode refChild)
        {
            return newChildren.Select(child => parentNode.InsertBefore(child, refChild)).ToList();
        }

        public static IEnumerable<HtmlNode> InsertAfter(this HtmlNode parentNode, IEnumerable<HtmlNode> newChildren, HtmlNode refChild)
        {
            return newChildren.Reverse().Select(child => parentNode.InsertAfter(child, refChild)).Reverse().ToList();
        }
    }
}
