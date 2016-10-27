﻿using System;
using System.Diagnostics;

using GameMover.Code;

using NUnit.Framework;

namespace Test
{
    public class TestBase
    {
        [OneTimeSetUp]
        public void Initial()
        {
            StaticMethods.LockActiveDirectory = false;

            ErrorHandling.HandleError = (message, exception, errorLevel) => {
                Console.WriteLine(message);
                throw exception;
            };

            StaticMethods.DisplayBusyDuring = action => {
                var stopwatch = Stopwatch.StartNew();
                action();
                Console.WriteLine($"Potentially long running action took {stopwatch.ElapsedMilliseconds}ms to complete.");
            };
        }
    }
}
