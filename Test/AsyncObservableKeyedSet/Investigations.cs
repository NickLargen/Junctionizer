using System;
using System.Diagnostics;
using System.Threading.Tasks;

using GameMover.ViewModels;

using NUnit.Framework;

namespace Test.AsyncObservableKeyedSet
{
    public class Investigations
    {
        [Test, Explicit]
        public async Task MultithreadingBehavior()
        {
            // RunInWPF count = 1_000_000 takes 20500ms (100%) SEND - thread safe
            // RunInWPF count = 1_000_000 takes 13500ms  (66%) POST - All items are available eventually, but there is a delay before values can be read
            // without sync c = 1_000_000 takes   400ms   (2%) NOT THREAD SAFE - ONlY 95-99% OF THE ELEMENTS END UP IN THE DICTIONARY
            // direct concurrentDictionary 200ms thread safe
            // direct dictionary 126ms not thread safe

            var stopwatch = Stopwatch.StartNew();

            await TestBase.RunInWpfSyncContext(Function);

            Console.WriteLine(stopwatch.ElapsedMilliseconds + "ms taken to execute.");

            // RunInWPF without Task.Run count = 1_000_000 with await 510ms
            // RunInWPF without Task.Run count = 1_000_000 straight add calls 380ms
        }


        private async Task Function()
        {
            var count = 1_000_000;
            var ints = new AsyncObservableKeyedSet<int, int>(i => i);

            var t1 = Task.Run(async () => {
                for (int i = 0; i < count; i++)
                {
                    await ints.AddAsync(i);
                }


                Console.WriteLine($"FINISHED ADDING FROM THE FIRST THREAD, {ints.Count} ITEMS IN DICTIONARY");
            });
            var t2 = Task.Run(async () => {
                for (int i = 0; i < count; i++)
                {
                    await ints.AddAsync(i + 1_000_000); //.ConfigureAwait(false);
                }


                Console.WriteLine($"FINISHED ADDING FROM THE second THREAD, {ints.Count} ITEMS IN DICTIONARY");
            });

            await Task.WhenAll(t1, t2);

            Console.WriteLine(ints.Count + " items");
        }
    }
}
