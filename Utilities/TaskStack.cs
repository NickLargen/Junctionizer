using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    /// <summary>Allows limiting the number of simultaneous running tasks- executes in a last in, first out order.</summary>
    public sealed class TaskStack : IDisposable
    {
        private readonly SemaphoreStack _semaphore;

        public TaskStack(int degreeParallelism)
        {
            _semaphore = new SemaphoreStack(degreeParallelism);
        }

        public async Task Enqueue(Func<Task> taskGenerator)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await taskGenerator().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose() => _semaphore.Dispose();

        private class SemaphoreStack : IDisposable
        {
            private SemaphoreSlim Semaphore { get; }
            private ConcurrentStack<TaskCompletionSource<bool>> Stack { get; } = new ConcurrentStack<TaskCompletionSource<bool>>();

            public SemaphoreStack(int initialCapacity)
            {
                Semaphore = new SemaphoreSlim(initialCapacity);
            }

            public Task WaitAsync()
            {
                var tcs = new TaskCompletionSource<bool>();
                Stack.Push(tcs);

                Semaphore.WaitAsync()
                         .ContinueWith(t => {
                             if (Stack.TryPop(out var popped))
                             {
                                 popped.SetResult(true);
                             }
                         });

                return tcs.Task;
            }

            public void Release() => Semaphore.Release();

            public void Dispose() => Semaphore.Dispose();
        }
    }
}
