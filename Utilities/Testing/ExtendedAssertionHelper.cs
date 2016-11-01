using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Utilities.Testing
{
    public class ExtendedAssertionHelper : AssertionHelper
    {
        /// <inheritdoc cref="AssertionHelper.Expect{T}(T,IResolveConstraint,string,object[])"/>
        /// <remarks> Delegating to a different name to emphasize that behavior of other Ensure overloads may differ in behavior to Expect, and to reduce the number of completion options Intellisense pops up with.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void Ensure<T>(T actual, IResolveConstraint constraint, string message = null, params object[] args)
        {
            Expect(actual, constraint, message, args);
        }

        /// <summary>Awaits the task in order to use that as the value - this avoids accidentally applying a constraint to a task, since performing operations assertions on Task objects is rarely the desired behavior. <inheritdoc cref="AssertionHelper.Expect{T}(T,IResolveConstraint,string,object[])"/></summary>
        protected async Task Ensure<T>(Task<T> task, IResolveConstraint constraint, string message = null, params object[] args)
        {
            Expect(await task, constraint, message, args);
        }

        /// <summary>Synchronously evaluates the provided task in order to assert on its value. <inheritdoc cref="Ensure{T}(T,IResolveConstraint,string,object[])"/></summary>
        protected void EnsureSynchronously<T>(Task<T> task, IResolveConstraint constraint, string message = null, params object[] args)
        {
            Expect(task.GetAwaiter().GetResult(), constraint, message, args);
        }

        protected async Task EnsureUsingThreadPool<T>(Func<Task<T>> asyncFunction, IResolveConstraint constraint, string message = null)
        {
            await Task.Run(() => Assert.That(() => asyncFunction(), constraint, message));
        }

        protected async Task HurlsUsingThreadPool<T>(Func<Task> asyncFunction) where T : Exception
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
        protected async Task Hurls<T>(Func<Task> function) where T : Exception
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

        /// <summary>Remove visual noise from the stack trace - we aren't debugging testing framework methods showing up</summary>
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
                        if (Regex.IsMatch(line, $".*{nameof(ExtendedAssertionHelper)}\\.cs.*")
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
