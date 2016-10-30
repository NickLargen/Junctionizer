using System;
using System.Text;
using System.Text.RegularExpressions;
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


        public static async Task HurlsUsingThreadPool<T>(Func<Task> asyncFunction) where T : Exception
        {
            await Hurls<T>(() => Task.Run(asyncFunction));
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
                throw new AssertionExceptionWithTrimmedStackTrace($"Expected: <{typeof(T)}>\n" +
                                                                  $" But was: <{e.GetType()}> {e.Message}\n"
                    , e.StackTrace);
            }

            throw new AssertionExceptionWithTrimmedStackTrace($"Expected: <{typeof(T)}> but no exceptions were encountered.\n");
        }

        /// <summary>
        /// Remove visual noise from the stack trace - we aren't debugging testing framework methods showing up
        /// </summary>
        public class AssertionExceptionWithTrimmedStackTrace : AssertionException
        {
            public string ReplacementStackTrace { get; }
            private const string END_OF_STACK_TRACE =
                "--- End of stack trace from previous location where exception was thrown ---";

            public AssertionExceptionWithTrimmedStackTrace(string message, string replacementStackTrace = null) : base(message)
            {
                ReplacementStackTrace = replacementStackTrace;
            }

            public override string StackTrace
            {
                get {
                    var stackLines = (ReplacementStackTrace ?? base.StackTrace).Split(new[] {Environment.NewLine}, StringSplitOptions.None);
                    var stringBuilder = new StringBuilder(100);

                    string lastLineAdded = null;

                    foreach (string line in stackLines)
                    {
                        var trimmedLine = line.Trim();
                        if (Regex.IsMatch(line, $".*{nameof(NUnit)}\\.cs.*")
                            || trimmedLine.StartsWith("at System.Runtime.CompilerServices.TaskAwaiter.", StringComparison.OrdinalIgnoreCase)
                            || trimmedLine.StartsWith("at NUnit.Framework.Internal.", StringComparison.OrdinalIgnoreCase)
                            || lastLineAdded == null && trimmedLine.Equals(END_OF_STACK_TRACE)) continue;

                        stringBuilder.AppendLine(line);
                        lastLineAdded = line;
                    }

                    if (lastLineAdded == END_OF_STACK_TRACE) stringBuilder.Remove(stringBuilder.Length - END_OF_STACK_TRACE.Length - 2, END_OF_STACK_TRACE.Length);

                    return stringBuilder.ToString();
                }
            }
        }
    }
}
