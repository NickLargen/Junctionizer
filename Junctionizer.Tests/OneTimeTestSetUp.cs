using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;

using NUnit.Framework;

[assembly: Timeout(10000)]

namespace Junctionizer.Tests
{
    [SetUpFixture]
    public class OneTimeTestSetUp
    {
        [OneTimeSetUp]
        public void Initial()
        {
            Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) => {
                ErrorHandling.HandleException(args.Exception);
                Debugger.Break();
            };

            TaskScheduler.UnobservedTaskException += (sender, unobservedTaskExceptionEventArgs) => {
                Debug.WriteLine(unobservedTaskExceptionEventArgs.Exception.Message);
                Debugger.Break();
                foreach (var exception in unobservedTaskExceptionEventArgs.Exception.InnerExceptions)
                {
                    ErrorHandling.HandleException(exception);
                }
            };

            StaticMethods.LockActiveDirectory = false;

            Dialogs.DisplayMessageBox = str => {
                Console.WriteLine(str);
                return null;
            };

            ErrorHandling.HandleError = (message, exception, errorLevel) => {
                Debug.WriteLine("Error handling received: " + exception.ToString());
                Console.WriteLine(message);
                throw exception;
            };

            StaticMethods.DisplayBusyDuring = action => {
//                var stopwatch = Stopwatch.StartNew();
                action();
//                Console.WriteLine($"Potentially long running action took {stopwatch.ElapsedMilliseconds}ms to complete.");
            };
        }
    }
}
