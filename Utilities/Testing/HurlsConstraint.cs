using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Utilities.Testing
{
    public class HurlsConstraint : PrefixConstraint
    {
        /// <summary>Initializes a new instance of the <see cref="HurlsConstraint"/> class, using a constraint to be applied to the exception.</summary>
        /// <param name="baseConstraint">A constraint to apply to the caught exception.</param>
        public HurlsConstraint(IConstraint baseConstraint) : base(baseConstraint) {}

        /// <summary><inheritdoc/>Appears in the test results as Expected: {Description}.</summary>
        public override string Description => BaseConstraint.Description;

        /// <summary>The maximum amount of time asynchronous code is allowed to run before throwing an exception. This allows deadlocked tests to fail without setting a global timeout.</summary>
        public static int TimeLimit { get; set; } = 5_000;

        /// <inheritdoc/>
        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            Exception caughtException = null;

            // If actual is Action or Func<T> where T is not a task
            var synchronousCode = actual as TestDelegate;

            // If actual is a Func<Task<T>> or Func<Task>
            var asyncCode = actual as AsyncTestDelegate;

            CancellationTokenSource waitForAsyncCodeTokenSource = new CancellationTokenSource();
            var token = waitForAsyncCodeTokenSource.Token;
            if (asyncCode != null)
            {
                // Async code could be run synchronously and returning something like Task.CompletedTask, so exceptions could either be propagated normally or be stored within the task.
                try
                {
                    asyncCode().ContinueWith(task => {
                        waitForAsyncCodeTokenSource.Cancel(true);
                        // Actual exception is wrapped in an AggregateException
                        caughtException = task.Exception?.InnerException;
                    }, token);
                }
                catch (Exception e)
                {
                    waitForAsyncCodeTokenSource.Cancel(true);
                    caughtException = e;
                }
            }
            else if (synchronousCode != null)
            {
                try
                {
                    waitForAsyncCodeTokenSource.Cancel(true);
                    synchronousCode();
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            }
            else
            {
                throw new ArgumentException(
                    $"The actual value must be a TestDelegate or AsyncTestDelegate but was {actual.GetType().Name}", nameof(actual));
            }

            Task.Delay(TimeLimit, token).ContinueWith(task => {
                    if(!task.IsCanceled) throw new InvalidOperationException($"Task did not complete within {TimeLimit}ms.");
                }).GetAwaiter().GetResult();

            return new HurlsConstraintResult(this, caughtException,
                caughtException != null
                    ? BaseConstraint.ApplyTo(caughtException)
                    : null);
        }

        private class HurlsConstraintResult : ConstraintResult
        {
            public HurlsConstraintResult(HurlsConstraint constraint, Exception caughtException, ConstraintResult baseResult)
                : base(constraint, caughtException)
            {
                if (caughtException != null && baseResult.IsSuccess) Status = ConstraintStatus.Success;
                else Status = ConstraintStatus.Failure;
            }

            public override void WriteActualValueTo(MessageWriter writer)
            {
                if (ActualValue == null) writer.Write("no exception thrown");
                else
                {
                    var e = (Exception) ActualValue;
                    writer.Write($"<{e.GetType()}> {e.Message}");
                }
            }
        }
    }

    public class HurlsOperator : ThrowsOperator
    {
        public override void Reduce(ConstraintBuilder.ConstraintStack stack)
        {
            // Disallow a plain Hurls.Exception, a type must be provided to avoid catching the incorrect exception
            if (stack.Empty) throw new InvalidOperationException();

            stack.Push(new HurlsConstraint(stack.Pop()));
        }
    }

    /// <summary>Helper class with properties and methods that supply constraints that operate on exceptions.</summary>
    public class Hurls
    {
        /// <summary>A new constraint specifying an expected exception.</summary>
        private static ResolvableConstraintExpression Exception => new ConstraintExpression().Append(new HurlsOperator());

        /// <summary>A new constraint specifying an exception with a given InnerException.</summary>
        private static ResolvableConstraintExpression InnerException => Exception.InnerException;

        /// <summary>A new constraint specifying the exact type of exception expected.</summary>
        public static ExactTypeConstraint TypeOf(Type expectedType) => Exception.TypeOf(expectedType);

        /// <inheritdoc cref="TypeOf"/>
        public static ExactTypeConstraint TypeOf<TExpected>() where TExpected : Exception => Exception.TypeOf<TExpected>();

        /// <summary>A new constraint specifying the type of exception expected, allowing derived types.</summary>
        public static InstanceOfTypeConstraint InstanceOf(Type expectedType) => Exception.InstanceOf(expectedType);

        /// <inheritdoc cref="InstanceOf"/>
        public static InstanceOfTypeConstraint InstanceOf<TExpected>() where TExpected : Exception => Exception.InstanceOf<TExpected>();

        /// <summary>A new constraint specifying that no exception is thrown.</summary>
        public static ThrowsNothingConstraint Nothing => new ThrowsNothingConstraint();

        /// <summary>A new constraint expecting an exception of type ArgumentException.</summary>
        public static ExactTypeConstraint ArgumentException => TypeOf<ArgumentException>();

        /// <summary>A new constraint expecting an exception of type ArgumentNullException.</summary>
        public static ExactTypeConstraint ArgumentNullException => TypeOf<ArgumentNullException>();

        /// <summary>A new constraint expecting an exception of type ArgumentOutOfRangeException.</summary>
        public static ExactTypeConstraint ArgumentOutOfRangeException => TypeOf<ArgumentOutOfRangeException>();

        /// <summary>A new constraint expecting an exception of type InvalidOperationException.</summary>
        public static ExactTypeConstraint InvalidOperationException => TypeOf<InvalidOperationException>();

        /// <summary>A new constraint expecting an exception of type KeyNotFoundException.</summary>
        public static ExactTypeConstraint KeyNotFoundException => TypeOf<KeyNotFoundException>();

        /// <summary>A new constraint expecting an exception of type TargetInvocationException.</summary>
        public static ExactTypeConstraint TargetInvocationException => TypeOf<TargetInvocationException>();
    }
}
