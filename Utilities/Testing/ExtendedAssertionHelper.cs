using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using JetBrains.Annotations;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using OneOf;

namespace Utilities.Testing
{
    public class ExtendedAssertionHelper : ConstraintFactory
    {
        #region Standard Assertions - Asserting on values

        /// <inheritdoc cref="Assert.That{T}(T,IResolveConstraint,string,object[])"/>
        /// <remarks> Delegating to a different name to emphasize that behavior of other Ensure overloads may differ in behavior to Assert.That, and to reduce the number of completion options Intellisense pops up with.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ensure<T>(T actual, IResolveConstraint constraint, string message = null, params object[] args)
        {
            Assert.That(actual, constraint, message, args);
        }

        /// <inheritdoc cref="Assert.That(bool, string, object[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ensure(bool condition, string message = null, params object[] args)
        {
            Assert.That(condition, message, args);
        }

        /// <summary>Synchronously waits for the task to complete in order to use that as the value - this avoids accidentally applying a constraint to a task, since performing assertions on Task objects is rarely the desired behavior. <inheritdoc cref="Assert.That{T}(T,IResolveConstraint,string,object[])"/></summary>
        public static void Ensure<T>([NotNull] Task<T> task, IResolveConstraint constraint, string message = null, params object[] args)
        {
            Assert.That(task.GetAwaiter().GetResult(), constraint, message, args);
        }

        /// <summary>Shortcut for <see cref="Ensure{T}(Task{T},IResolveConstraint,string,object[])"/> with a constraint of Is.True</summary>
        public static void Ensure([NotNull] Task<bool> conditionTask, string message = null, params object[] args)
        {
            Assert.That(conditionTask.GetAwaiter().GetResult(), message, args);
        }

        #endregion


        #region Exception Handling - Asserting on functions and actions that should throw an assertion

        /// <summary>Asserts that the code represented by a delegate throws an exception that satisfies the constraint provided.</summary>
        public static void EnsureException<T>([NotNull] Func<T> function, OneOf<TypeConstraint, ThrowsNothingConstraint> constraint,
            string message = null, params object[] args)
        {
            var typedConstraint = constraint.Match<IResolveConstraint>(t1 => t1, t2 => t2);

            var asyncFunction = function as Func<Task>;
            if (asyncFunction != null)
            {
                Assert.That(new AsyncTestDelegate(asyncFunction), typedConstraint, message, args);
            }
            else
            {
                Assert.That(new TestDelegate(() => function()), typedConstraint, message, args);
            }
        }

        /// <inheritdoc cref="EnsureException{T}"/>
        public static void EnsureException([NotNull] Action action, OneOf<TypeConstraint, ThrowsNothingConstraint> constraint,
            string message = null, params object[] args)
        {
            var typedConstraint = constraint.Match<IResolveConstraint>(t1 => t1, t2 => t2);

            Assert.That(new TestDelegate(action), typedConstraint, message, args);
        }

        #endregion


        /// <summary>
        /// Alternative to Assert.Throws that allows assertion propagation between threads using await.
        /// <para/>
        /// Usage:
        /// <code>
        ///     await HurlsException&lt;NotSupportedException>(()=>{throw new NotSupportedException();});
        /// </code>
        /// </summary>
        /// <typeparam name="T">The expected exception.</typeparam>
        public async Task HurlsException<T>(Func<Task> asyncAction) where T : Exception
        {
            try
            {
                await asyncAction();
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
