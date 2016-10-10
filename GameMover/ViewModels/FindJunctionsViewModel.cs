using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

using GameMover.Code;
using GameMover.External_Code;

using MaterialDesignThemes.Wpf;

using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace GameMover.ViewModels
{

    public class FindJunctionsViewModel : BindableBase, INotification
    {

        public FindJunctionsViewModel()
        {
            Content = this;
        }

        public async Task ExecuteSearch(string selectedPath)
        {
            var stopwatch = Stopwatch.StartNew();

//            var cancellationTokenSource = new CancellationTokenSource();
            var mainDispatcher = Dispatcher.CurrentDispatcher;

            var numDirectories = 0;

            var junctionFindTask = Task.Run(() => {
                Parallel.ForEach(new DirectoryInfo(selectedPath).EnumerateAllAccessibleDirectories(), info => {
//                    if (cancellationTokenSource.Token.IsCancellationRequested) return;

                    numDirectories++;
                    CurrentFolder = info.FullName;
                    // Parent could be null if it is a root directory
                    if (JunctionPoint.Exists(info) && info.Parent != null)
                    {
                        junctions.Add(info);
                    }
                });

                mainDispatcher.Invoke(() => {
                    DialogHost.CloseDialogCommand.Execute(false, null);
                });
            });

            await junctionFindTask;
//            cancellationTokenSource.Dispose();

//            StaticMethods.DisplayError(stopwatch.ElapsedMilliseconds / 1000f + " " + numDirectories);
        }

        public string CurrentFolder { get; set; } = string.Empty;
        public List<DirectoryInfo> junctions { get; } = new List<DirectoryInfo>();

        /// <inheritdoc />
        public string Title { get; set; } = "SEARCHING!!!!";
        /// <inheritdoc />
        public object Content { get; set; }

    }

}
