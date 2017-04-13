using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;


using NUnit.Framework;

using Utilities;
using Utilities.Collections;

namespace Junctionizer.Tests.AsyncObservableKeyedSet
{
    public class Investigations
    {
        public async Task MultithreadingBehavior()
        {
            // RunInWPF count = 1_000_000 takes 20500ms (100%) SEND - thread safe
            // RunInWPF count = 1_000_000 takes 13500ms  (66%) POST - All items are available eventually, but there is a delay before values can be read
            // RunInWPF count = 1_000_000 takes   221ms   (1%) AddAllAsync - thread safe
            // without sync c = 1_000_000 takes   400ms   (2%) NOT THREAD SAFE - ONlY 95-99% OF THE ELEMENTS END UP IN THE DICTIONARY
            // direct concurrentDictionary 200ms thread safe
            // direct dictionary 126ms not thread safe

            var stopwatch = Stopwatch.StartNew();

            await RunInWpfSyncContext(Function);

            Console.WriteLine(stopwatch.ElapsedMilliseconds + "ms taken to execute.");

            // RunInWPF without Task.Run count = 1_000_000 with await 510ms
            // RunInWPF without Task.Run count = 1_000_000 straight add calls 380ms
        }

        public static async Task RunInWpfSyncContext(Func<Task> function)
        {
            if (function == null) throw new ArgumentNullException("function");

            var prevCtx = SynchronizationContext.Current;
            try
            {
                var syncCtx = new DispatcherSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                var task = function();
                if (task == null) throw new InvalidOperationException();

                var frame = new DispatcherFrame();
                task.ContinueWith(x => {
                    frame.Continue = false;
                }, TaskScheduler.Default).Forget();
                Dispatcher.PushFrame(frame); // execute all tasks until frame.Continue == false

                await task; // rethrow exception when task has failed 
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
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
