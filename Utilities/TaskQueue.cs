using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public sealed class TaskQueue : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public TaskQueue(int degreeParallelism)
        {
            _semaphore = new SemaphoreSlim(degreeParallelism);
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
    }
}
