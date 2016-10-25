using System.Threading.Tasks;

using static GameMover.Code.ErrorHandling;

namespace GameMover.UI
{
    public partial class App
    {
        public App()
        {
            Current.DispatcherUnhandledException += (sender, args) => {
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
    }
}
