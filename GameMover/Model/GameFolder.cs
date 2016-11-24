using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GameMover.Code;

using JetBrains.Annotations;

using Microsoft.VisualStudio.Threading;

using Prism.Mvvm;

using Utilities.Comparers;

using static GameMover.Code.ErrorHandling;

namespace GameMover.Model
{
    [DebuggerDisplay(nameof(GameFolder) + " {" + nameof(DirectoryInfo) + "}")]
    public class GameFolder : BindableBase, IComparable, IComparable<GameFolder>
    {
        public GameFolder([NotNull] string fullPath) : this(new DirectoryInfo(fullPath)) {}

        public GameFolder([NotNull] DirectoryInfo directory)
        {
            DirectoryInfo = directory;
            IsJunction = JunctionPoint.Exists(directory);
            if (IsJunction) JunctionTarget = JunctionPoint.GetTarget(directory);

            UpdatePropertiesFromSubdirectories().Forget();
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
        public long Size { get; private set; } = -1;

        public bool IsBeingDeleted { get; set; }
        public bool HasFinalSize => !IsSizeOutdated && !IsContinuoslyRecalculating;

        private bool IsContinuoslyRecalculating { get; set; }
        private bool IsSizeOutdated { get; set; }

        [CanBeNull] private CancellationTokenSource _propertyUpdateTokenSource;

        public void CancelSubdirectorySearch() => SafeCancelTokenSource(_propertyUpdateTokenSource);

        private async Task UpdatePropertiesFromSubdirectories()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                // Cancel the previous update task (if it exists) and save a reference to the current tokenSource so that it can be cancelled later
                SafeCancelTokenSource(Interlocked.Exchange(ref _propertyUpdateTokenSource, tokenSource));

                IsSizeOutdated = true;

                var cancellationToken = tokenSource.Token;
                try
                {
                    await TaskQueueDictionary.GetOrAdd(DirectoryInfo.Root.Name, new TaskQueue(2))
                                             .Enqueue(() => {
                                                 return Task.Run(() => SearchSubdirectories(cancellationToken), cancellationToken);
                                             }, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // That's fine, do nothing
                }

                // If there are no other updates on the task queue null out the token source since we no longer need it
                Interlocked.CompareExchange(ref _propertyUpdateTokenSource, null, tokenSource);
            }
        }

        private void SearchSubdirectories(CancellationToken cancellationToken)
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
                        cancellationToken.ThrowIfCancellationRequested();

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

        public Task RecalculateSize() => UpdatePropertiesFromSubdirectories();

        /// <summary>Periodically calls <see cref="RecalculateSize"/> until it is determined that the size is not longer changing.</summary>
        /// <returns></returns>
        public async Task ContinuoslyRecalculateSize()
        {
            IsContinuoslyRecalculating = true;
            long? oldSize;
            do
            {
                oldSize = Size;
                Debug.WriteLine($"{DirectoryInfo.FullName} oldSize {oldSize}  Size {Size}");
                await Task.Delay(1500);

                await UpdatePropertiesFromSubdirectories();
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

        public void Rename([NotNull] string newName)
        {
            Debug.Assert(DirectoryInfo.Parent != null);
            DirectoryInfo = new DirectoryInfo(Path.Combine(DirectoryInfo.Parent.FullName, newName));
        }
    }
}
