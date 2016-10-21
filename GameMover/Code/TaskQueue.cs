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

        public async Task<T> Enqueue<T>(Func<Task<T>> taskGenerator)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await taskGenerator();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task Enqueue(Func<Task> taskGenerator)
        {
            await _semaphore.WaitAsync();
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
