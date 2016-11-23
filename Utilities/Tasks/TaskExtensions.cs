using System.Threading.Tasks;

namespace Utilities.Tasks
{
    public static class TaskExtensions
    {
        public static void RunTaskSynchronously(this Task task) => task.GetAwaiter().GetResult();

        public static T RunTaskSynchronously<T>(this Task<T> task) => task.GetAwaiter().GetResult();
    }
}
