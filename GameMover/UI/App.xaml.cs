using System.Threading.Tasks;

using static GameMover.Code.ErrorHandling;

namespace GameMover.UI
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        /// <inheritdoc />
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
