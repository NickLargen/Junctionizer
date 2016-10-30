using System;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Utilities.Testing
{
    public static class NUnit
    {
        public static async Task EnsureUsingThreadPool<T>(Func<Task<T>> asyncFunction, IResolveConstraint constraint)
        {
            await Task.Run(() => Assert.That(() => asyncFunction(), constraint));
        }

        //A small abstraction for our testing framework.
        public static void Ensure<T>(T actual, IResolveConstraint constraint)
        {
            Assert.That(actual, constraint);
        }

        public static void Ensure<T>([NotNull] Task<T> task, IResolveConstraint constrant)
        {
            Assert.That(task.GetAwaiter().GetResult(), constrant);
        }

        /// <summary>
        /// Alternative to Assert.Throws that allows assertion propagation between threads using await.
        /// <para/>
        /// Usage:
        /// <code>
        ///     await Hurls&lt;NotSupportedException>(()=>{throw new NotSupportedException();});
        /// </code>
        /// </summary>
        /// <typeparam name="T">The expected exception.</typeparam>
        /// <param name="function">The </param>
        /// <returns></returns>
        public static async Task Hurls<T>(Func<Task> function) where T : Exception
        {
            try
            {
                await function();
            }
            catch (T)
            {
                // $"Caught expected exception {typeof(T)}."
                return;
            }
            catch (Exception e)
            {
                Assert.Fail($"Expected: <{typeof(T)}>\n" +
                            $" But was: <{e.GetType()}> {e.Message}\n\n" +
                            $"            FULL STACK TRACE\n" +
                            $"----------------------------------------\n" +
                            $"{e}\n" +
                            $"----------------------------------------\n" +
                            $"             END STACK TRACE\n\n");
            }

            throw new AssertionExceptionWithTrimmedStackTrace($"Expected: <{typeof(T)}> but no exceptions were encountered.\n");
        }

        public class AssertionExceptionWithTrimmedStackTrace : AssertionException
        {
            public AssertionExceptionWithTrimmedStackTrace(string message) : base(message) {}

            public override string StackTrace
            {
                get {
                    var stackLines = base.StackTrace.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
                    var stringBuilder = new StringBuilder(100);
                    for (int i = 0; i < stackLines.Length; i++)
                    {
                        stringBuilder.AppendLine(stackLines[i]);
                    }

                    return stringBuilder.ToString();
                }
            }
        }
    }
}
