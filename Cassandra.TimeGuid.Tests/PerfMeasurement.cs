using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Core.Tests.Commons.ObjectsTests.TimeGuidTests
{
    public static class PerfMeasurement
    {
        public static void Do([NotNull] string actionName, int threadsCount, int totalIterationsCount, [NotNull] Action action)
        {
            var iterationsPerThread = totalIterationsCount / threadsCount;
            var threads = new List<Thread>();
            var startSignal = new ManualResetEvent(false);
            for (var t = 0; t < threadsCount; t++)
            {
                var thread = new Thread(() =>
                    {
                        startSignal.WaitOne();
                        for (var i = 0; i < iterationsPerThread; i++)
                            action();
                    });
                thread.Start();
                threads.Add(thread);
            }
            for (var i = 0; i < 1000; i++) // warmup
                action();
            var sw = Stopwatch.StartNew();
            startSignal.Set();
            threads.ForEach(thread => thread.Join());
            sw.Stop();
            Console.Out.WriteLine("{0} took {1} ms to make {2} iterations in {3} threads", actionName, sw.ElapsedMilliseconds, totalIterationsCount, threadsCount);
        }
    }
}