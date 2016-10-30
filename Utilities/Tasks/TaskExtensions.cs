using System.Threading.Tasks;

namespace Utilities.Tasks
{
    public static class TaskExtensions
    {
        public static void GetAwaiterResult(this Task task)
        {
            task.GetAwaiter().GetResult();
        }

        public static T GetAwaiterResult<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}
