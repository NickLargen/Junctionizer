using System;
using System.Linq;
using System.Threading.Tasks;

using GameMover.ViewModels;

using Nito.AsyncEx;

using NUnit.Framework;

using static Utilities.Testing.NUnit;

namespace Test.AsyncObservableKeyedSet
{
    /// <summary>The intent is that in normal execution the lack of a synchronization context causes <see cref="AsyncObservableKeyedSet{TKey,TItem}.RunOnSynchronizationContext"/> to be a noop, so <see cref="RunningOnWpfSynchronizationContext"/> will run them all with a context so that both behaviors are tested.</summary>
    public class AsyncObservableKeyedSetUnitTests
    {
        [Test]
        public void RunningOnWpfSynchronizationContext()
        {
            AsyncContext.Run(async () => {
                Console.WriteLine("----- Executing tests using a synchronization context -----");
                foreach (
                    var methodInfo in GetType()
                        .GetMethods()
                        .Where(info => info.CustomAttributes.Any(data => data.AttributeType == typeof(TestAttribute)))
                        .Where(info => !string.Equals(info.Name, nameof(RunningOnWpfSynchronizationContext))))
                {
                    Console.WriteLine($"Running {methodInfo.Name}...");
                    await (Task) methodInfo.Invoke(this, Array.Empty<object>());
                    Console.WriteLine($"- Finished {methodInfo.Name}\n");
                }
            });
        }

        [Test]
        public async Task AddDuplicateKeyThrowsException()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => 0, new[] {1});

            await HurlsUsingThreadPool<ArgumentException>(() => set.AddAsync(463));
        }

        [Test]
        public async Task AddDuplicateThrowsException()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param, new[] {1});

            await HurlsUsingThreadPool<ArgumentException>(() => set.AddAsync(1));
        }

        [Test]
        public Task BasicKeyIndexedAccess()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param / 5, new[] {15, 20, 25});

            Ensure(set[3], Is.EqualTo(15));
            Ensure(set[5], Is.EqualTo(25));

            return Task.CompletedTask;
        }

        [Test]
        public async Task BasicRemoveKey()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param / 5, new[] {15, 20, 25});

            Ensure(set.Count, Is.EqualTo(3));
            await EnsureUsingThreadPool(() => set.RemoveKeyAsync(3), Is.True);
            Ensure(set.Count, Is.EqualTo(2));
            Ensure(set.RemoveKeyAsync(67532), Is.False);
            Ensure(set.Count, Is.EqualTo(2));
            Ensure(set.RemoveKeyAsync(15), Is.False);
            Ensure(set.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task ClearRemovesAllEntries()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param, new[] {1, 436, 768, 79876, 5, 435, 4634, 243, 2});

            Ensure(set, Is.Not.Empty);
            await Task.Run(() => set.ClearAsync());
            Ensure(set, Is.Empty);
            Ensure(set.Count, Is.Zero);

            foreach (var _ in set)
            {
                Assert.Fail();
            }
        }

        [Test]
        public async Task TryAddDuplicateItemReturnsFalse()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param, new[] {1, 2, 3});

            await EnsureUsingThreadPool(() => set.TryAddAsync(2), Is.False);
            Ensure(set.Count, Is.EqualTo(3));
            Ensure(set.TryAddAsync(67532), Is.True);
            Ensure(set.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task TryAddDuplicateKeyReturnsFalse()
        {
            var set = new AsyncObservableKeyedSet<int, int>(param => param % 5, new[] {15});

            await EnsureUsingThreadPool(() => set.TryAddAsync(30), Is.False);
            Ensure(set.Count, Is.EqualTo(1));
            await EnsureUsingThreadPool(() => set.TryAddAsync(31), Is.True);
            await EnsureUsingThreadPool(() => set.TryAddAsync(31), Is.False);
            await EnsureUsingThreadPool(() => set.TryAddAsync(33), Is.True);
            await EnsureUsingThreadPool(() => set.TryAddAsync(33), Is.False);
            await EnsureUsingThreadPool(() => set.TryAddAsync(33), Is.False);
            Ensure(set.Count, Is.EqualTo(3));

            Ensure(set.TryAddAsync(30), Is.False);
            await EnsureUsingThreadPool(() => Task.Run(() => set.Count), Is.EqualTo(3));
            Ensure(set.TryAddAsync(32), Is.True);
            Ensure(set.Count, Is.EqualTo(4));
        }

        /*[Test]
        public async Task Reentrancy()
        {
            var set = new AsyncObservableKeyedSet<string, string>(param => param);

            await Task.Run(() => {
                var i = 0;
                set.CollectionChanged += (sender, args) => {
                    i++;
                    set.AddAsync(i.ToString());
                };
                set.CollectionChanged += (sender, args) => {
                    Console.WriteLine(
                        "Modifying a collection within a CollectionChanged event while there are two or more CollectionChanged listeners should not work, even in purely single threaded circumstances.");
                };
            });

            await HurlsUsingThreadPool<InvalidOperationException>(() => set.AddAsync("-1"));
        }*/
    }
}
