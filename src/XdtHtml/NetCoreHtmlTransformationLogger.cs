using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace XdtHtml
{
    public class NetCoreHtmlTransformationLogger : IHtmlTransformationLogger
    {
        #region private data members
        private readonly bool continueOnError;

        private readonly ILogger externalLogger;

        private readonly Queue<IDisposable> openScopes = new Queue<IDisposable>();
        #endregion

        public NetCoreHtmlTransformationLogger(ILogger logger, bool continueOnError = true) {
            this.externalLogger = logger;
            this.continueOnError = continueOnError;
        }

        public void LogMessage(string message, params object[] messageArgs)
        {
            externalLogger.LogInformation(message, messageArgs);
        }

        public void LogMessage(MessageType type, string message, params object[] messageArgs)
        {
            externalLogger.Log(type == MessageType.Normal ? LogLevel.Information : LogLevel.Debug, message, messageArgs);
        }

        public void LogWarning(string message, params object[] messageArgs)
        {
            externalLogger.LogWarning(message, messageArgs);
        }

        public void LogWarning(int lineNumber, int linePosition, string message, params object[] messageArgs)
        {
            externalLogger.LogWarning($"({lineNumber}, {linePosition}): {message}", messageArgs);
        }

        public void LogError(string message, params object[] messageArgs)
        {
            externalLogger.LogError(message, messageArgs);

            if (!this.continueOnError)
            {
                throw new HtmlTransformationException(String.Format(CultureInfo.CurrentCulture, message, messageArgs));
            }
        }

        public void LogError(int lineNumber, int linePosition, string message, params object[] messageArgs)
        {
            externalLogger.LogError($"({lineNumber}, {linePosition}): {message}", messageArgs);

            if (!this.continueOnError)
                throw new HtmlNodeException(String.Format(CultureInfo.CurrentCulture, message, messageArgs), lineNumber, linePosition);
        }

        public void LogErrorFromException(Exception ex)
        {
            externalLogger.LogError(ex, ex.ToString());

            if (this.continueOnError)
            {
                throw ex;
            }
        }

        public void LogErrorFromException(Exception ex, int lineNumber, int linePosition)
        {
            externalLogger.LogError(ex, $"({lineNumber}, {linePosition}): {ex}");

            if (this.continueOnError)
            {
                throw ex;
            }
        }

        public void StartSection(string message, params object[] messageArgs)
        {
            this.LogMessage(message, messageArgs);
            openScopes.Enqueue(externalLogger.BeginScope(message, messageArgs));
        }

        public void StartSection(MessageType type, string message, params object[] messageArgs)
        {
            this.LogMessage(type, message, messageArgs);
            openScopes.Enqueue(externalLogger.BeginScope(message, messageArgs));
        }

        public void EndSection(string message, params object[] messageArgs)
        {
            openScopes.Dequeue().Dispose();
            this.LogMessage(message, messageArgs);
        }

        public void EndSection(MessageType type, string message, params object[] messageArgs)
        {
            openScopes.Dequeue().Dispose();
            this.LogMessage(type, message, messageArgs);
        }
    }
}
