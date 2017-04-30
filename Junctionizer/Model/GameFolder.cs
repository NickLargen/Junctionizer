using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Prism.Mvvm;

using Utilities;
using Utilities.Comparers;

using static Junctionizer.ErrorHandling;

namespace Junctionizer.Model
{
    [DebuggerDisplay(nameof(GameFolder) + " {" + nameof(DirectoryInfo) + "}")]
    public class GameFolder : BindableBase, IComparable, IComparable<GameFolder>
    {
        public const int UNKNOWN_SIZE = -1;
        public const int JUNCTION_POINT_SIZE = -2;

        public GameFolder([NotNull] string fullPath, PauseToken pauseToken) : this(new DirectoryInfo(fullPath), pauseToken) { }

        public GameFolder([NotNull] DirectoryInfo directory, PauseToken pauseToken)
        {
            DirectoryInfo = directory;
            PauseToken = pauseToken;
            IsJunction = JunctionPoint.Exists(directory);
            if (IsJunction)
            {
                Size = JUNCTION_POINT_SIZE;
                JunctionTarget = JunctionPoint.GetTarget(directory);
            }

            UpdatePropertiesFromSubdirectoriesAsync().Forget();
        }

        [NotNull]
        private static ConcurrentDictionary<string, TaskQueue> TaskQueueDictionary { get; } = new ConcurrentDictionary<string, TaskQueue>();

        [NotNull]
        public DirectoryInfo DirectoryInfo { get; set; }
        [NotNull]
        public string Name => DirectoryInfo.Name;
        [CanBeNull]
        public string JunctionTarget { get; }
        public DateTime LastWriteTime { get; private set; } = DateTime.MinValue;
        public bool IsJunction { get; }
        public long Size { get; private set; } = UNKNOWN_SIZE;

        public bool IsBeingDeleted { get; set; }
        public bool HasFinalSize => !IsSizeOutdated && !IsContinuoslyRecalculating;

        private bool IsContinuoslyRecalculating { get; set; }
        private bool IsSizeOutdated { get; set; }

        private PauseToken PauseToken { get; }
        [CanBeNull] private CancellationTokenSource _propertyUpdateTokenSource;

        public void CancelSubdirectorySearch() => SafeCancelTokenSource(_propertyUpdateTokenSource);

        private async Task UpdatePropertiesFromSubdirectoriesAsync()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                // Cancel the previous update task (if it exists) and save a reference to the current tokenSource so that it can be cancelled later
                SafeCancelTokenSource(Interlocked.Exchange(ref _propertyUpdateTokenSource, tokenSource));

                IsSizeOutdated = true;

                var cancellationToken = tokenSource.Token;
                await TaskQueueDictionary.GetOrAdd(DirectoryInfo.Root.Name, new TaskQueue(2))
                                         .Enqueue(() => {
                                             // Handle cancellation without incurring exception overhead
                                             return cancellationToken.IsCancellationRequested
                                                        ? Task.CompletedTask
                                                        // ReSharper disable once MethodSupportsCancellation
                                                        : Task.Run(() => SearchSubdirectoriesAsync(cancellationToken));
                                         }, cancellationToken)
                                         .ConfigureAwait(false);

                // If there are no other updates on the task queue null out the token source since we no longer need it
                Interlocked.CompareExchange(ref _propertyUpdateTokenSource, null, tokenSource);
            }
        }

        private async Task SearchSubdirectoriesAsync(CancellationToken cancellationToken)
        {
            // This method may be called simultaneously from multiple threads

            try
            {
                if (IsJunction)
                {
                    LastWriteTime = DirectoryInfo.LastWriteTime;
                }
                else
                {
                    long tempSize = 0;
                    foreach (var info in DirectoryInfo.EnumerateAllAccessibleDirectories())
                    {
                        if (PauseToken.IsPaused) await PauseToken.WaitWhilePausedAsync();
                        if (cancellationToken.IsCancellationRequested) return;

                        if (info.LastWriteTime > LastWriteTime) LastWriteTime = info.LastWriteTime;

                        tempSize += info.EnumerateFiles().Sum(fileInfo => fileInfo.Length);

                        if (tempSize > Size) Size = tempSize;
                    }

                    Size = tempSize;
                }

                IsSizeOutdated = false;
            }
            catch (IOException e)
            {
                HandleException(e);
            }
        }

        public Task RecalculateSizeAsync() => UpdatePropertiesFromSubdirectoriesAsync();

        /// <summary>Periodically calls <see cref="RecalculateSizeAsync"/> until it is determined that the size is not longer changing.</summary>
        /// <returns></returns>
        public async Task ContinuoslyRecalculateSizeAsync()
        {
            IsContinuoslyRecalculating = true;
            long? oldSize;
            do
            {
                oldSize = Size;
                Debug.WriteLine($"{DirectoryInfo.FullName} oldSize {oldSize}  Size {Size}");
                await Task.Delay(1500).ConfigureAwait(false);

                await UpdatePropertiesFromSubdirectoriesAsync().ConfigureAwait(false);
            } while (Size != oldSize);

            IsContinuoslyRecalculating = false;
        }

        /// <inheritdoc/>
        int IComparable.CompareTo(object obj) => CompareTo((GameFolder) obj);

        public int CompareTo(GameFolder other)
        {
            return NaturalStringComparer.CompareOrdinal(DirectoryInfo.FullName, other?.DirectoryInfo.FullName, ignoreCase: true);
        }

        public override string ToString() => $"{nameof(DirectoryInfo)}: {DirectoryInfo.FullName}";

        /// <summary>Sets the name of the GameFolder but does not affect the file system.</summary>
        public void Rename([NotNull] string newName)
        {
            Debug.Assert(DirectoryInfo.Parent != null);
            DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryInfo.Parent.FullName, newName));
        }
    }
}
