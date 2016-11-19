using System;
using System.Diagnostics;

using JetBrains.Annotations;

namespace Utilities
{
    [PublicAPI]
    public static class Chronometer
    {
        public static void DebugWriteElapsedTime(Action action) => Debug.WriteLine(GetElapsedTime(action));

        public static void ConsoleWriteElapsedTime(Action action) => Console.WriteLine(GetElapsedTime(action));

        public static long GetElapsedTime(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
    }
}
