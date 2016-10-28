using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

using Utilities.Strings;

namespace GameMover.Code
{
    public static class ErrorHandling
    {
        public enum ErrorLevel
        {
            Error,
            Warning
        }

        public delegate void ErrorHandler(string message, Exception e = null, ErrorLevel errorLevel = ErrorLevel.Error);

        private static MessageBoxImage GetImage(ErrorLevel errorLevel)
        {
            switch (errorLevel)
            {
                case ErrorLevel.Error:
                    return MessageBoxImage.Error;
                case ErrorLevel.Warning:
                    return MessageBoxImage.Warning;
                default:
                    throw new ArgumentOutOfRangeException(nameof(errorLevel), errorLevel, null);
            }
        }

        public static ErrorHandler HandleError { private get; set; } = (message, exception, errorLevel) => {
            if (exception?.InnerException?.Message != null) message += " " + exception.InnerException.Message.SubstringUntil(" (Exception from");

#if DEBUG
            Debug.WriteLine(exception?.ToString() ?? message);
            var stackTrace = exception?.StackTrace;

            exception = exception?.InnerException;
            while (exception != null)
            {
                message += Environment.NewLine + "INNER EXCEPTION: " + exception.Message;
                exception = exception.InnerException;
            }

            message += Environment.NewLine + stackTrace;
#endif

            MessageBox.Show(message, errorLevel.ToString(), MessageBoxButton.OK, GetImage(errorLevel));
        };

        public static void HandleException(Exception exception)
        {
            var ioException = exception as IOException;
            if (ioException != null)
            {
                // Provide a useful message if the error was from a drive failure
                var maybeFullPath = ioException.GetType()
                                               .GetField("_maybeFullPath",
                                                   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                               ?.GetValue(ioException);

                var message = ioException.Message;
                if (maybeFullPath != null)
                {
                    message += $" '{maybeFullPath}'";
                }
                HandleError(message, ioException, ErrorLevel.Warning);
            }
            else
            {
                HandleError(exception.Message, exception);
            }
        }

        /// <summary>Cancels the provided token source if it is not null and has not yet been disposed.</summary>
        /// <param name="cancellationTokenSource"></param>
        public static void SafeCancelTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore
            }
        }

        /// <exception cref="DirectoryNotFoundException"></exception>
        public static void CheckLocationExists(string loc)
        {
            if (!Directory.Exists(loc)) throw new DirectoryNotFoundException($"Could not find directory '{loc}'.");
        }
    }
}
