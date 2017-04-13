using System.Threading.Tasks;

namespace Utilities
{
    public static class TaskExtensions
    {
        public static void RunTaskSynchronously(this Task task) => task.GetAwaiter().GetResult();

        public static T RunTaskSynchronously<T>(this Task<T> task) => task.GetAwaiter().GetResult();

        /// <summary>Consumes a task and doesn't do anything with it.  Useful for fire-and-forget calls to async methods within async methods.</summary>
        /// <param name="task">The task whose result is to be ignored.</param>
        public static void Forget(this Task task) { }
    }
}
