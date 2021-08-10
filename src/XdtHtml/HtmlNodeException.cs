using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using HtmlAgilityPack;

namespace XdtHtml
{
    [Serializable]
    public sealed class HtmlNodeException : HtmlTransformationException
    {
        private readonly int lineNumber;
        private readonly int linePosition;

        public static Exception Wrap(Exception ex, HtmlNode node) {
            if (ex is HtmlNodeException) {
                // If this is already an XmlNodeException, then it probably
                // got its node closer to the error, making it more accurate
                return ex;
            }
            else {
                return new HtmlNodeException(ex, node);
            }
        }

        public static Exception Wrap(Exception ex, HtmlAttribute attribute)
        {
            if (ex is HtmlNodeException)
            {
                // If this is already an XmlNodeException, then it probably
                // got its node closer to the error, making it more accurate
                return ex;
            }
            else
            {
                return new HtmlNodeException(ex, attribute);
            }
        }

        public HtmlNodeException(Exception innerException, HtmlNode node)
            : this(innerException, node.Line, node.LinePosition) {
        }

        public HtmlNodeException(Exception innerException, HtmlAttribute attribute)
            : this(innerException, attribute.Line, attribute.LinePosition)
        {
        }

        public HtmlNodeException(Exception innerException, int lineNumber, int linePosition)
            : base(innerException.Message, innerException)
        {
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }

        public HtmlNodeException(string message, HtmlNode node)
            : this(message, node.Line, node.LinePosition) {
        }

        public HtmlNodeException(string message, HtmlAttribute attribute)
            : this(message, attribute.Line, attribute.LinePosition)
        {
        }

        public HtmlNodeException(string message, int lineNumber, int linePosition)
            : base(message)
        {
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }

        public bool HasErrorInfo {
            get {
                return lineNumber > 0;
            }
        }

        public int LineNumber {
            get {
                return lineNumber;
            }
        }

        public int LinePosition {
            get {
                return linePosition;
            }
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("lineNumber", lineNumber);
            info.AddValue("linePosition", linePosition);
        }
    }
}
