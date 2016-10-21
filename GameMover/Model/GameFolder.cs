using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using GameMover.Code;

using Prism.Mvvm;

namespace GameMover.Model
{

    [DebuggerDisplay(nameof(GameFolder) + " {" + nameof(DirectoryInfo) + "}")]
    public class GameFolder : BindableBase, IComparable<GameFolder>, IEquatable<GameFolder>
    {

        public GameFolder(string fullPath) : this(new DirectoryInfo(fullPath)) {}

        public GameFolder(DirectoryInfo directory)
        {
            DirectoryInfo = directory;
            IsJunction = JunctionPoint.Exists(directory);
            if (IsJunction) JunctionTarget = JunctionPoint.GetTarget(directory);

            UpdatePropertiesFromSubdirectories();
        }

        private static ConcurrentDictionary<string, TaskQueue> TaskQueueDictionary { get; } = new ConcurrentDictionary<string, TaskQueue>();

        public DirectoryInfo DirectoryInfo { get; private set; }
        public string Name => DirectoryInfo.Name;
        public string JunctionTarget { get; }
        public DateTime? LastWriteTime { get; private set; }
        public bool IsJunction { get; }
        public long? Size { get; private set; }

        private CancellationTokenSource TokenSource { get; set; }

        public bool IsSearchingSubdirectories => TokenSource != null;

        public void CancelSubdirectorySearch() => TokenSource?.Cancel();

        private async void UpdatePropertiesFromSubdirectories()
        {
            if (TokenSource != null)
            {
                CancelSubdirectorySearch();
                while (IsSearchingSubdirectories)
                {
                    await Task.Delay(25);
                }
            }

            Size = null;
            LastWriteTime = null;

            await TaskQueueDictionary.GetOrAdd(DirectoryInfo.Root.Name, new TaskQueue(2))
                                     .Enqueue(() => {
                                         TokenSource = new CancellationTokenSource();
                                         var cancellationToken = TokenSource.Token;

                                         return Task.Run(() => SearchSubdirectories(cancellationToken), cancellationToken);
                                     });

            if (TokenSource != null)
            {
                var ts = TokenSource;
                TokenSource = null;
                ts.Dispose();
            }
        }

        private void SearchSubdirectories(CancellationToken cancellationToken)
        {
            LastWriteTime = DirectoryInfo.LastWriteTime;

            if (!IsJunction)
            {
                StaticMethods.HandleIOExceptionsDuring(() => {
                    Size = 0;
                    foreach (var info in DirectoryInfo.EnumerateAllAccessibleDirectories())
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        if (info.LastWriteTime > LastWriteTime) LastWriteTime = info.LastWriteTime;

                        foreach (var fileInfo in info.EnumerateFiles())
                        {
                            Size += fileInfo.Length;
                        }
                    }
                });
            }
        }

        public void RecalculateSize() => UpdatePropertiesFromSubdirectories();

        public void Rename(string newName)
        {
            DirectoryInfo = new DirectoryInfo(DirectoryInfo.Parent?.FullName + @"\" + newName);
        }

        public int CompareTo(GameFolder other) => other == null
                                                      ? 1
                                                      : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => CompareTo(obj as GameFolder) == 0;
        public bool Equals(GameFolder other) => CompareTo(other) == 0;
        public static bool operator ==(GameFolder left, GameFolder right) => Equals(left, right);
        public static bool operator !=(GameFolder left, GameFolder right) => !Equals(left, right);

        public override int GetHashCode() => Name.ToLowerInvariant().GetHashCode();

    }

}
