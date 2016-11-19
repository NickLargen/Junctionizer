using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;

namespace TestingUtilities
{
    public class ExtendedAssertionHelper : ConstraintFactory
    {
        #region Standard Assertions - Asserting on values

        /// <inheritdoc cref="Assert.That{T}(T,IResolveConstraint,string,object[])"/>
        /// <remarks> Delegating to a different name to emphasize that behavior of other Ensure overloads may differ in behavior to Assert.That, and to reduce the number of completion options Intellisense pops up with.</remarks>
        public static void Ensure<T>(T actual, IConstraint constraint, string message = null, params object[] args)
        {
            constraint = constraint.Resolve();

            TestExecutionContext.CurrentContext.IncrementAssertCount();
            var result = constraint.ApplyTo(actual);
            if (!result.IsSuccess)
            {
                MessageWriter writer = new TextMessageWriter(message, args);
                result.WriteMessageTo(writer);
                // If an exception was thrown its stack trace should be displayed, not the stack trace of this assertion exception.
                throw new AssertionExceptionWithTrimmedStackTrace(writer.ToString(), (result.ActualValue as Exception)?.StackTrace);
            }
        }

        /// <summary>Shortcut for <see cref="Ensure{T}(T,IConstraint,string,object[])"/> with a constraint of Is.True</summary>
        public static void Ensure(bool condition, string message = null, params object[] args)
        {
            Ensure(condition, Is.True, message, args);
        }

        /// <summary>Synchronously waits for the task to complete in order to use that as the value - this avoids accidentally applying a constraint to a task, since performing assertions on Task objects is rarely the desired behavior.</summary>
        public static void Ensure<T>([NotNull] Task<T> task, IConstraint constraint, string message = null, params object[] args)
        {
            Ensure(task.GetAwaiter().GetResult(), constraint, message, args);
        }

        /// <summary>Shortcut for <see cref="Ensure{T}(Task{T},IConstraint,string,object[])"/> with a constraint of Is.True</summary>
        public static void Ensure([NotNull] Task<bool> conditionTask, string message = null, params object[] args)
        {
            Ensure(conditionTask, Is.True, message, args);
        }

        #endregion


        #region Exception Handling - Asserting on functions and actions that should throw an assertion

        /// <summary>Asserts that the code represented by a delegate throws an exception that satisfies the constraint provided.</summary>
        public static void EnsureException<T>([NotNull] Func<T> function, IConstraint constraint,
            string message = null, params object[] args)
        {
            var asyncFunction = function as Func<Task>;
            if (asyncFunction != null)
            {
                Ensure(new AsyncTestDelegate(asyncFunction), constraint, message, args);
            }
            else
            {
                Ensure(new TestDelegate(() => function()), constraint, message, args);
            }
        }

        /// <inheritdoc cref="EnsureException{T}"/>
        public static void EnsureException([NotNull] Action action, IConstraint constraint,
            string message = null, params object[] args)
        {
            Ensure(new TestDelegate(action), constraint, message, args);
        }

        #endregion


        /// <summary>Remove visual noise from the stack trace - we don't need to see testing framework methods.</summary>
        public class AssertionExceptionWithTrimmedStackTrace : AssertionException
        {
            private const string END_OF_STACK_TRACE_PREVIOUS_LOCATION =
                "--- End of stack trace from previous location where exception was thrown ---";

            private string[] NamespacesToIgnore { get; } = {
                typeof(ExtendedAssertionHelper).Namespace,
                "System.Runtime.CompilerServices.TaskAwaiter.",
                "NUnit.Framework.Internal.",
                "System.ThrowHelper"
            };

            public string ReplacementStackTrace { get; }

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

                    foreach (string line in stackLines
                        .Select(line => line.TrimStart())
                        .Where(line => NamespacesToIgnore.All(
                            ignorableNamespace => !line.StartsWith($"at {ignorableNamespace}", StringComparison.OrdinalIgnoreCase)))
                        // If an entire section of the stacktrace has been removed, don't start with a divider
                        .Where(line => lastLineAdded != null || !line.Equals(END_OF_STACK_TRACE_PREVIOUS_LOCATION)))
                    {
                        stringBuilder.AppendLine(line);
                        lastLineAdded = line;
                    }

                    if (lastLineAdded == END_OF_STACK_TRACE_PREVIOUS_LOCATION)
                    {
                        stringBuilder.Remove(stringBuilder.Length - END_OF_STACK_TRACE_PREVIOUS_LOCATION.Length - 2,
                            END_OF_STACK_TRACE_PREVIOUS_LOCATION.Length);
                    }

                    return stringBuilder.ToString();
                }
            }
        }
    }
}
