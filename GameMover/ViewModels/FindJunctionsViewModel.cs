using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using GameMover.Code;
using GameMover.External_Code;

using Prism.Mvvm;

namespace GameMover.ViewModels
{

    public class FindJunctionsViewModel : BindableBase
    {
        public void Cancel() => TokenSource?.Cancel();

        private CancellationTokenSource TokenSource { get; set; }

        public string CurrentFolder { get; private set; } = string.Empty;
        public int NumDirectories { get; private set; }
        public int NumJunctions { get; private set; }

        public async Task<List<DirectoryInfo>> GetJunctions(string selectedPath)
        {
            var junctions = new List<DirectoryInfo>();

            TokenSource = new CancellationTokenSource();
            var cancellationToken = TokenSource.Token;

            NumDirectories = 0;
            NumJunctions = 0;

            await Task.Run(() => {
                foreach (var info in new DirectoryInfo(selectedPath).EnumerateAllAccessibleDirectories())
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    NumDirectories++;
                    CurrentFolder = info.FullName;
                    // Parent could be null if it is a root directory
                    if (JunctionPoint.Exists(info) && info.Parent != null)
                    {
                        junctions.Add(info);
                        NumJunctions++;
                    }
                }

                CurrentFolder = "Complete!";
            }, cancellationToken);

            var ts = TokenSource;
            TokenSource = null;
            ts.Dispose();

            return junctions;
        }

    }

}
