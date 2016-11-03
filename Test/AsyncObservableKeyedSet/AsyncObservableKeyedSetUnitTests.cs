using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GameMover.ViewModels;

using Nito.AsyncEx;

using NUnit.Framework;

using Utilities.Tasks;
using Utilities.Testing;

namespace Test.AsyncObservableKeyedSet
{
    /// <summary>The intent is that in normal execution the lack of a synchronization context causes <see cref="AsyncObservableKeyedSet{TKey,TItem}.RunOnSynchronizationContext"/> to be a noop, so <see cref="RunningWithSynchronizationContext"/> will run them all with a context so that both behaviors are tested.</summary>
    public class AsyncObservableKeyedSetUnitTests : ExtendedAssertionHelper
    {
        private SynchronizationContext Context { get; set; }

        [SetUp]
        public void SetUp()
        {
            // Make a context isn't carried over from a previous test.
            Context = null;
        }

        [Test]
        public void RunningWithSynchronizationContext()
        {
            AsyncContext.Run(() => {
                Context = AsyncContext.Current.SynchronizationContext;

                // Run tests on a different thread than the synchronization context to avoid deadlocks
                return Task.Run(() => {
                    Console.WriteLine("----- Executing tests using a synchronization context -----");
                    foreach (
                        var methodInfo in GetType()
                            .GetMethods()
                            .Where(info => info.CustomAttributes.Any(data => data.AttributeType == typeof(TestAttribute)))
                            .Where(info => !string.Equals(info.Name, nameof(RunningWithSynchronizationContext))))
                    {
                        Console.WriteLine($"Running {methodInfo.Name}...");
                        methodInfo.Invoke(this, Array.Empty<object>());
                        Console.WriteLine($"- Finished {methodInfo.Name}\n");
                    }
                });
            });
        }

        [Test]
        public void AddDuplicateKeyThrowsException()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => 0, Context, new[] {1});

            Ensure(set.TryAddAsync(1), Is.False);
            EnsureException(() => set.AddAsync(13), Throws.ArgumentException);
        }

        [Test]
        public void AddDuplicateThrowsException()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param, Context, new[] {1});

            EnsureException(() => set.AddAsync(1), Throws.ArgumentException);
        }

        [Test]
        public void BasicKeyIndexedAccess()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param / 5, Context, new[] {15, 20, 25});

            Ensure(set[3], Is.EqualTo(15));
            Ensure(set[5], Is.EqualTo(25));
        }

        [Test]
        public void BasicRemoveKey()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param / 5, Context, new[] {15, 20, 25});

            Ensure(set.Count, Is.EqualTo(3));
            Ensure(set.RemoveKeyAsync(3), Is.True);
            Ensure(set.Count, Is.EqualTo(2));
            Ensure(set.RemoveKeyAsync(67532), Is.False);
            Ensure(set.Count, Is.EqualTo(2));
            Ensure(set.RemoveKeyAsync(15), Is.False);
            Ensure(set.Count, Is.EqualTo(2));
        }

        [Test]
        public void ClearRemovesAllEntries()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param, Context, new[] {1, 436, 768, 79876, 5, 435, 4634, 243, 2});

            Ensure(set, Is.Not.Empty);
            Task.Run(set.ClearAsync).GetAwaiterResult();
            Ensure(set, Is.Empty);
            Ensure(set.Count, Is.Zero);

            foreach (var _ in set)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void TryAddDuplicateItemReturnsFalse()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param, Context, new[] {1, 2, 3});

            Ensure(set.TryAddAsync(2), Is.False);
            Ensure(set.Count, Is.EqualTo(3));
            Ensure(set.TryAddAsync(67532), Is.True);
            Ensure(set.Count, Is.EqualTo(4));
        }

        [Test]
        public void TryAddDuplicateKeyReturnsFalse()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param % 5, Context, new[] {15});

            Ensure(set.TryAddAsync(30), Is.False);
            Ensure(set.Count, Is.EqualTo(1));
            Ensure(set.TryAddAsync(31), Is.True);
            Ensure(set.TryAddAsync(31), Is.False);
            Ensure(set.TryAddAsync(33), Is.True);
            Ensure(set.TryAddAsync(33), Is.False);
            Ensure(set.TryAddAsync(33), Is.False);
            Ensure(set.Count, Is.EqualTo(3));

            Ensure(set.TryAddAsync(30), Is.False);
            Ensure(Task.Run(() => set.Count), Is.EqualTo(3));
            Ensure(set.TryAddAsync(32), Is.True);
            Ensure(set.Count, Is.EqualTo(4));
        }

        /*[Test]
        public void Reentrancy()
        {
            var set = new AsyncObservableKeyedSet<string, string>(param => param, Context);

            var i = 0;
            set.CollectionChanged += (sender, args) => {
                i++;
                if (i < 10) set.AddAsync(i.ToString());
            };
            set.CollectionChanged += (sender, args) => {
                Console.WriteLine(
                    "Modifying a collection within a CollectionChanged event while there are two or more CollectionChanged listeners should not work, even in purely single threaded circumstances.");
            };

            EnsureException(() => set.AddAsync("-1"), Throws.InvalidOperationException);
        }*/
    }
}
