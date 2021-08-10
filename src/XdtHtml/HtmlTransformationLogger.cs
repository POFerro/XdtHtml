using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using HtmlAgilityPack;

namespace XdtHtml
{
    public class HtmlTransformationLogger
    {
        #region private data members
        private bool hasLoggedErrors = false;

        private IHtmlTransformationLogger externalLogger;
        private HtmlAttribute currentReferenceAttribute = null;

        private bool fSupressWarnings = false;
        #endregion

        internal HtmlTransformationLogger(IHtmlTransformationLogger logger) {
            this.externalLogger = logger;
        }

        internal void LogErrorFromException(Exception ex) {
            hasLoggedErrors = true;

            if (externalLogger != null) {
                HtmlNodeException nodeException = ex as HtmlNodeException;
                if (nodeException != null && nodeException.HasErrorInfo) {
                    externalLogger.LogErrorFromException(
                        nodeException,
                        //ConvertUriToFileName(nodeException.FileName),
                        nodeException.LineNumber,
                        nodeException.LinePosition);
                }
                else {
                    externalLogger.LogErrorFromException(ex);
                }
            }
            else {
                throw ex;
            }
        }

        internal bool HasLoggedErrors {
            get {
                return hasLoggedErrors;
            }
            set {
                hasLoggedErrors = false;
            }
        }

        internal HtmlAttribute CurrentReferenceAttribute {
            get {
                return currentReferenceAttribute;
            }
            set {
                // I don't feel like implementing a stack for this for no
                // reason. Only one thing should try to set this property
                // at a time, and that thing should clear it when done.
                Debug.Assert(currentReferenceAttribute == null || value == null, "CurrentReferenceAttribute is being overwritten");

                currentReferenceAttribute = value;
            }
        }

        #region public interface

        public bool SupressWarnings
        {
            get { return fSupressWarnings; }
            set { fSupressWarnings = value; }
        }

        public void LogMessage(string message, params object[] messageArgs) {
            if (externalLogger != null) {
                externalLogger.LogMessage(message, messageArgs);
            }
        }

        public void LogMessage(MessageType type, string message, params object[] messageArgs) {
            if (externalLogger != null) {
                externalLogger.LogMessage(type, message, messageArgs);
            }
        }

        public void LogWarning(string message, params object[] messageArgs) {
            if (SupressWarnings)
            {
                // SupressWarnings downgrade the Warning to LogMessage
                LogMessage(message, messageArgs);
            }
            else
            {
                if (CurrentReferenceAttribute != null)
                {
                    LogWarning(CurrentReferenceAttribute, message, messageArgs);
                }
                else if (externalLogger != null)
                {
                    externalLogger.LogWarning(message, messageArgs);
                }
            }
        }

        public void LogWarning(HtmlAttribute referenceAttr, string message, params object[] messageArgs) {
            if (SupressWarnings)
            {
                // SupressWarnings downgrade the Warning to LogMessage
                LogMessage(message, messageArgs);
            }
            else
            {
                if (externalLogger != null)
                {
                    //string fileName = ConvertUriToFileName(referenceNode.OwnerDocument);
                    //IXmlLineInfo lineInfo = referenceNode as IXmlLineInfo;

                    //if (lineInfo != null)
                    //{
                        externalLogger.LogWarning(
                            //fileName,
                            referenceAttr.Line,
                            referenceAttr.LinePosition,
                            message,
                            messageArgs);
                    //}
                    //else
                    //{
                    //    externalLogger.LogWarning(
                    //        fileName,
                    //        message,
                    //        messageArgs);
                    //}
                }
            }
        }

        public void LogWarning(HtmlNode referenceNode, string message, params object[] messageArgs)
        {
            if (SupressWarnings)
            {
                // SupressWarnings downgrade the Warning to LogMessage
                LogMessage(message, messageArgs);
            }
            else
            {
                if (externalLogger != null)
                {
                    //string fileName = ConvertUriToFileName(referenceNode.OwnerDocument);
                    //IXmlLineInfo lineInfo = referenceNode as IXmlLineInfo;

                    //if (lineInfo != null)
                    //{
                    externalLogger.LogWarning(
                        //fileName,
                        referenceNode.Line,
                        referenceNode.LinePosition,
                        message,
                        messageArgs);
                    //}
                    //else
                    //{
                    //    externalLogger.LogWarning(
                    //        fileName,
                    //        message,
                    //        messageArgs);
                    //}
                }
            }
        }

        public void LogError(string message, params object[] messageArgs) {
            hasLoggedErrors = true;

            if (CurrentReferenceAttribute != null) {
                LogError(CurrentReferenceAttribute, message, messageArgs);
            }
            else if (externalLogger != null) {
                externalLogger.LogError(message, messageArgs);
            }
            else {
                throw new HtmlTransformationException(String.Format(CultureInfo.CurrentCulture, message, messageArgs));
            }
        }

        public void LogError(HtmlAttribute referenceAttr, string message, params object[] messageArgs) {
            hasLoggedErrors = true;

            if (externalLogger != null) {
                //string fileName = ConvertUriToFileName(referenceNode.OwnerDocument);
                //IXmlLineInfo lineInfo = referenceNode as IXmlLineInfo;

                //if (lineInfo != null) {
                    externalLogger.LogError(
                        //fileName,
                        referenceAttr.Line,
                        referenceAttr.LinePosition,
                        message,
                        messageArgs);
                //}
                //else {
                //    externalLogger.LogError(
                //        fileName,
                //        message,
                //        messageArgs);
                //}
            }
            else {
                throw new HtmlNodeException(String.Format(CultureInfo.CurrentCulture, message, messageArgs), referenceAttr);
            }
        }

        public void LogError(HtmlNode referenceNode, string message, params object[] messageArgs)
        {
            hasLoggedErrors = true;

            if (externalLogger != null)
            {
                //string fileName = ConvertUriToFileName(referenceNode.OwnerDocument);
                //IXmlLineInfo lineInfo = referenceNode as IXmlLineInfo;

                //if (lineInfo != null) {
                externalLogger.LogError(
                    //fileName,
                    referenceNode.Line,
                    referenceNode.LinePosition,
                    message,
                    messageArgs);
                //}
                //else {
                //    externalLogger.LogError(
                //        fileName,
                //        message,
                //        messageArgs);
                //}
            }
            else
            {
                throw new HtmlNodeException(String.Format(CultureInfo.CurrentCulture, message, messageArgs), referenceNode);
            }
        }

        public void StartSection(string message, params object[] messageArgs) {
            if (externalLogger != null) {
                externalLogger.StartSection(message, messageArgs);
            }
        }

        public void StartSection(MessageType type, string message, params object[] messageArgs) {
            if (externalLogger != null) {
                externalLogger.StartSection(type, message, messageArgs);
            }
        }

        public void EndSection(string message, params object[] messageArgs) {
            if (externalLogger != null) {
                externalLogger.EndSection(message, messageArgs);
            }
        }

        public void EndSection(MessageType type, string message, params object[] messageArgs) {
            if (externalLogger != null) {
                externalLogger.EndSection(type, message, messageArgs);
            }
        }

        #endregion

        //private string ConvertUriToFileName(HtmlDocument htmlDocument) {
        //    string uri;
        //    XmlFileInfoDocument errorInfoDocument = htmlDocument as XmlFileInfoDocument;
        //    if (errorInfoDocument != null) {
        //        uri = errorInfoDocument.FileName;
        //    }
        //    else {
        //        uri = htmlDocument.BaseURI;
        //    }

        //    return ConvertUriToFileName(uri);
        //}

        //private string ConvertUriToFileName(string fileName) {
        //    try {
        //        Uri uri = new Uri(fileName);
        //        if (uri.IsFile && String.IsNullOrEmpty(uri.Host)) {
        //            fileName = uri.LocalPath;
        //        }
        //    }
        //    catch (UriFormatException) {
        //        // Bad URI format, so just return the original filename
        //    }

        //    return fileName;
        //}
    }
}
