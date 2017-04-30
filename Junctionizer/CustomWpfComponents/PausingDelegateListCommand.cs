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

    public class PausingDelegateListCommand<T> : DelegateListCommandBase<T>
    {
        [NotNull]
        private PauseTokenSource TokenSource { get; }

        [NotNull]
        private Func<T, Task> IndividualExecuteMethod { get; }

        /// <inheritdoc/>
        public PausingDelegateListCommand(Func<IEnumerable<T>> applicableItemsFunc, [NotNull] Func<T, Task> individualExecuteMethod, [CanBeNull] PauseTokenSource tokenSource)
            : base(applicableItemsFunc)
        {
            IndividualExecuteMethod = individualExecuteMethod;
            TokenSource = tokenSource ?? new PauseTokenSource();
        }

        /// <inheritdoc/>
        protected override void Execute(object parameter)
        {
            Task.Run(ExecuteAsync);
        }

        private async Task ExecuteAsync()
        {
            List<Task> newTasks;
            lock (TokenSource)
            {
                TokenSource.IsPaused = true;
                newTasks = ApplicableItemsFunc().Select(IndividualExecuteMethod).ToList();
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
