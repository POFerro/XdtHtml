using System;
using System.Collections.Generic;
using System.Text;

namespace XdtHtml
{
    // This doesn't do anything, except mark an error as having come from
    // the transformation engine
    [Serializable]
    public class HtmlTransformationException : Exception
    {
        public HtmlTransformationException(string message)
            : base(message) {
        }

        public HtmlTransformationException(string message, Exception innerException)
            : base(message, innerException) {
        }
    }
}
