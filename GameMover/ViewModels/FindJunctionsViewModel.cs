using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using GameMover.Code;

using Prism.Mvvm;

namespace GameMover.ViewModels
{
    public class FindJunctionsViewModel : BindableBase
    {
        private CancellationTokenSource TokenSource { get; set; }

        public bool IsSearching { get; private set; }
        public string CurrentFolder { get; private set; }
        public int NumDirectories { get; private set; }
        public int NumJunctions { get; private set; }

        public void Cancel() => ErrorHandling.SafeCancelTokenSource(TokenSource);

        public async Task<List<DirectoryInfo>> GetJunctions(string selectedPath)
        {
            var junctions = new List<DirectoryInfo>();
            IsSearching = true;

            NumDirectories = 0;
            NumJunctions = 0;

            using (TokenSource = new CancellationTokenSource())
            {
                var cancellationToken = TokenSource.Token;

                try
                {
                    await Task.Run(() => {
                        foreach (var info in new DirectoryInfo(selectedPath).EnumerateAllAccessibleDirectories())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            NumDirectories++;
                            CurrentFolder = info.FullName;
                            // Parent could be null if it is a root directory
                            if (JunctionPoint.Exists(info) && info.Parent != null)
                            {
                                junctions.Add(info);
                                NumJunctions++;
                            }
                        }
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // No work needed when user cancels search
                }
            }

            IsSearching = false;

            return junctions;
        }
    }
}
