using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GameMover.Code
{
    public static class ErrorHandling
    {
        static ErrorHandling()
        {
            Application.Current.DispatcherUnhandledException += (sender, args) => {
                HandleException(args.Exception);
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (sender, unobservedTaskExceptionEventArgs) => {
                foreach (var exception in unobservedTaskExceptionEventArgs.Exception.InnerExceptions)
                {
                    HandleException(exception);
                }
            };
        }

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

        public static ErrorHandler HandleError { get; set; } = (message, exception, errorLevel) => {
            MessageBox.Show(message, errorLevel.ToString(), MessageBoxButton.OK, GetImage(errorLevel));
            Debug.WriteLine(exception);
        };

        public static void HandleException(Exception exception)
        {
            var ioException = exception as IOException;
            if (ioException != null)
            {
                // Provide a useful message if the error was from a drive failure
                var message = ioException.Message;
                var maybeFullPath = ioException.GetType()
                                               .GetField("_maybeFullPath",
                                                   BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (maybeFullPath != null)
                {
                    message += $" '{maybeFullPath.GetValue(ioException)}'";
                }
                HandleError(message, ioException, ErrorLevel.Warning);
            }
            else
            {
                HandleError(exception.Message, exception);
            }
        }

        /// <summary>
        ///     Cancels the provided token source if it is not null and has not yet been disposed.
        /// </summary>
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
