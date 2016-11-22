using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using GameMover.Code;

using Microsoft.VisualStudio.Threading;

using NUnit.Framework;

[assembly: Timeout(1000)]

namespace GameMover.Tests
{
    public class TestBase
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

            ErrorHandling.HandleError = (message, exception, errorLevel) => {
                Debug.WriteLine("Error handling received: " + exception.ToString());
                Console.WriteLine(message);
                throw exception;
            };

            StaticMethods.DisplayBusyDuring = action => {
                var stopwatch = Stopwatch.StartNew();
                action();
                Console.WriteLine($"Potentially long running action took {stopwatch.ElapsedMilliseconds}ms to complete.");
            };
        }

        public static async Task RunInWpfSyncContext(Func<Task> function)
        {
            if (function == null) throw new ArgumentNullException("function");

            var prevCtx = SynchronizationContext.Current;
            try
            {
                var syncCtx = new DispatcherSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                var task = function();
                if (task == null) throw new InvalidOperationException();

                var frame = new DispatcherFrame();
                task.ContinueWith(x => {
                        frame.Continue = false;
                    }, TaskScheduler.Default).Forget();
                Dispatcher.PushFrame(frame); // execute all tasks until frame.Continue == false

                await task; // rethrow exception when task has failed 
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }
    }
}
