using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using AngleSharp.Dom;

namespace XdtHtml
{
    [Serializable]
    public sealed class HtmlNodeException : HtmlTransformationException
    {
        private readonly int? lineNumber;
        private readonly int? linePosition;

        public static Exception Wrap(Exception ex, INode node)
        {
            return node is IElement elem ?
                Wrap(ex, elem) : 
                node is IAttr attr ? Wrap(ex, attr) : 
                throw new NotSupportedException(node.GetType().Name + " não é suportado");
        }

        public static Exception Wrap(Exception ex, IElement node) {
            if (ex is HtmlNodeException) {
                // If this is already an XmlNodeException, then it probably
                // got its node closer to the error, making it more accurate
                return ex;
            }
            else {
                return new HtmlNodeException(ex, node);
            }
        }

        public static Exception Wrap(Exception ex, IAttr attribute)
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

        public HtmlNodeException(Exception innerException, IElement node)
            : this(innerException, node.SourceReference?.Position.Line, node.SourceReference?.Position.Column) {
        }

        public HtmlNodeException(Exception innerException, IAttr attribute)
            : this(innerException, attribute.OwnerElement.SourceReference?.Position.Line, attribute.OwnerElement.SourceReference?.Position.Column)
        {
        }

        public HtmlNodeException(Exception innerException, int? lineNumber, int? linePosition)
            : base(innerException.Message, innerException)
        {
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }

        public HtmlNodeException(string message, IElement node)
            : this(message, node.SourceReference?.Position.Line, node.SourceReference?.Position.Column)
        {
        }

        public HtmlNodeException(string message, IAttr attribute)
            : this(message, attribute.OwnerElement.SourceReference?.Position.Line, attribute.OwnerElement.SourceReference?.Position.Column)
        {
        }

        public HtmlNodeException(string message, int? lineNumber, int? linePosition)
            : base(message)
        {
            this.lineNumber = lineNumber;
            this.linePosition = linePosition;
        }

        public bool HasLineInfo {
            get {
                return lineNumber != null && linePosition != null;
            }
        }

        public int? LineNumber {
            get {
                return lineNumber;
            }
        }

        public int? LinePosition {
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
