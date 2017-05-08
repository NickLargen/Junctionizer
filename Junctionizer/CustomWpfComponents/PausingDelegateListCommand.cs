using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Utilities;
using Utilities.Collections;

namespace Junctionizer.CustomWpfComponents
{
    internal static class TaskPausingTracker
    {
        internal static MultiValueDictionary<PauseTokenSource, Task> RunningTasks { get; } = new MultiValueDictionary<PauseTokenSource, Task>();
    }

    public class PausingListCommand<T> : ListCommandBase<T>
    {
        [NotNull]
        private PauseTokenSource TokenSource { get; }

        [NotNull]
        private Func<T, Task> IndividualExecuteMethod { get; }
        public Func<IList<T>, Task<bool>> ShouldExecuteFunc { get; }

        /// <inheritdoc/>
        public PausingListCommand(Func<IEnumerable<T>> applicableItemsFunc, [NotNull] Func<T, Task> individualExecuteMethod, [CanBeNull] PauseTokenSource tokenSource, Func<IList<T>, Task<bool>> shouldExecuteFunc = null)
            : base(applicableItemsFunc)
        {
            IndividualExecuteMethod = individualExecuteMethod;
            TokenSource = tokenSource ?? new PauseTokenSource();
            ShouldExecuteFunc = shouldExecuteFunc;
        }

        /// <inheritdoc/>
        public override void Execute(object parameter)
        {
            Task.Run(ExecuteAsync);
        }

        private async Task ExecuteAsync()
        {
            IList<T> applicableItems = ApplicableItemsFunc().ToList();

            if (ShouldExecuteFunc != null)
            {
                var shouldExecute = await ShouldExecuteFunc(applicableItems);
                if (!shouldExecute) return;
            }

            List<Task> newTasks;
            lock (TokenSource)
            {
                TokenSource.IsPaused = true;
                newTasks = applicableItems.Select(IndividualExecuteMethod).ToList();
                TaskPausingTracker.RunningTasks.AddRange(TokenSource, newTasks);
            }

            await Task.WhenAll(newTasks);

            lock (TokenSource)
            {
                TaskPausingTracker.RunningTasks.RemoveRange(TokenSource, newTasks);
                if (!TaskPausingTracker.RunningTasks.ContainsKey(TokenSource)) TokenSource.IsPaused = false;
            }
        }
    }
}
