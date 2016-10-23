using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameMover.Code
{
    public class TaskQueue
    {
        private readonly SemaphoreSlim _semaphore;

        public TaskQueue(int degreeParallelism)
        {
            _semaphore = new SemaphoreSlim(degreeParallelism);
        }

        public async Task Enqueue(Func<Task> taskGenerator, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await taskGenerator();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
