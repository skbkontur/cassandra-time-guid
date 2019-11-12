using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using NUnit.Framework;

namespace SkbKontur.Cassandra.TimeBasedUuid.Tests
{
    [TestFixture]
    public class PreciseTimestampGeneratorTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            QuerySystemTimerResolution();
            Console.Error.WriteLine($"isCloudCiEnvironment: {isCloudCiEnvironment}");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(64)]
        [TestCase(128)]
        [Category("LongRunning")]
        public void PreciseTimestampGenerator_Perf(int threadsCount)
        {
            const int totalIterationsCount = 64 * 1000 * 1000;
            var sut = CreateNewTimestampGenerator();
            PerfMeasurement.Do("PreciseTimestampGenerator.Now()", threadsCount, totalIterationsCount, () => sut.NowTicks());
        }

        [Test]
        public void PreciseTimestampGenerator_Collisions()
        {
            const int count = 32 * 1000 * 1000;
            var timestampGenerator = CreateNewTimestampGenerator();
            var results = new HashSet<long>();
            for (var i = 0; i < count; i++)
                results.Add(timestampGenerator.NowTicks());
            Assert.That(results.Count, Is.EqualTo(count));
        }

        [Test]
        public void EnsureMicrosecondResolution()
        {
            const int count = 1000 * 1000;
            var timeSeries = Enumerable.Range(0, count).Select(x => PreciseTimestampGenerator.Instance.NowTicks()).ToArray();
            for (var i = 1; i < count; i++)
                Assert.That(timeSeries[i] - timeSeries[i - 1], Is.GreaterThanOrEqualTo(10));
        }

        [TestCaseSource(nameof(sleepDurations))]
        public void EnsureWallTimeLikelihood(TimeSpan sleepDuration, int iterations)
        {
            var actualDurations = new TimeSpan[iterations];
            RunPerfCriticalCode(() =>
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var beforeSleep = PreciseTimestampGenerator.Instance.NowTicks();
                        SleepWithHihResolution(sleepDuration);
                        var afterSleep = PreciseTimestampGenerator.Instance.NowTicks();
                        actualDurations[i] = TimeSpan.FromTicks(afterSleep - beforeSleep);
                    }
                });
            AssertThatDurationIsEqualTo(actualDurations, sleepDuration);
        }

        [TestCaseSource(nameof(sleepDurations))]
        public void EnsureStopwatchAndWallTimeLikelihood(TimeSpan sleepDuration, int iterations)
        {
            Assert.That(Stopwatch.IsHighResolution);
            var actualDurations = new TimeSpan[iterations];
            RunPerfCriticalCode(() =>
                {
                    var sw = Stopwatch.StartNew();
                    for (var i = 0; i < iterations; i++)
                    {
                        sw.Restart();
                        SleepWithHihResolution(sleepDuration);
                        sw.Stop();
                        actualDurations[i] = sw.Elapsed;
                    }
                });
            AssertThatDurationIsEqualTo(actualDurations, sleepDuration);
        }

        [TestCaseSource(nameof(sleepDurations))]
        public void GetDateTimeTicks(TimeSpan sleepDuration, int iterations)
        {
            var actualDurations = new TimeSpan[iterations];
            RunPerfCriticalCode(() =>
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var startTimestamp = Stopwatch.GetTimestamp();
                        SleepWithHihResolution(sleepDuration);
                        var endTimestamp = Stopwatch.GetTimestamp();
                        var stopwatchTicks = endTimestamp - startTimestamp;
                        actualDurations[i] = TimeSpan.FromTicks(PreciseTimestampGenerator.GetDateTimeTicks(stopwatchTicks));
                    }
                });
            AssertThatDurationIsEqualTo(actualDurations, sleepDuration);
        }

        private static void RunPerfCriticalCode(Action action)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking : true);
            if (!GC.TryStartNoGCRegion(10_000_000))
                throw new InvalidOperationException("Failed to enter no GC region latency mode");

            SetSystemTimerResolution(desiredResolutionMs : 0.5);

            action();

            ResetSystemTimerResolution();

            GC.EndNoGCRegion();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking : true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SleepWithHihResolution(TimeSpan sleepDuration)
        {
            var sw = Stopwatch.StartNew();
            var largeSleepDuration = sleepDuration - smallSleepDuration;
            if (largeSleepDuration.Ticks > 0)
                Thread.Sleep(largeSleepDuration);
            while (sw.Elapsed < sleepDuration)
                Thread.SpinWait(100);
        }

        private static void AssertThatDurationIsEqualTo(TimeSpan[] actualDurations, TimeSpan expectedDuration)
        {
            var actualDurationsStr = $"actualDurations: {string.Join(", ", actualDurations)}";

            var meanEps = TimeSpan.FromMilliseconds(1);
            var meanActualDuration = TimeSpan.FromMilliseconds(actualDurations.Average(x => x.TotalMilliseconds));
            Assert.That(meanActualDuration,
                        Is.InRange(expectedDuration - meanEps, expectedDuration + meanEps),
                        $"meanActualDuration: {meanActualDuration} deviates from expectedDuration: {expectedDuration}; {actualDurationsStr}");

            Array.Sort(actualDurations);
            var errorTolerance = isCloudCiEnvironment ? 1 : 0;
            var maxEps = TimeSpan.FromMilliseconds(2);
            Assert.That(actualDurations[0 + errorTolerance],
                        Is.GreaterThan(expectedDuration - maxEps),
                        $"minActualDuration: {actualDurations[0 + errorTolerance]} deviates from expectedDuration: {expectedDuration}; {actualDurationsStr}");
            Assert.That(actualDurations[actualDurations.Length - 1 - errorTolerance],
                        Is.LessThan(expectedDuration + maxEps),
                        $"maxActualDuration: {actualDurations[actualDurations.Length - 1 - errorTolerance]} deviates from expectedDuration: {expectedDuration}; {actualDurationsStr}");
        }

        private static PreciseTimestampGenerator CreateNewTimestampGenerator()
        {
            return new PreciseTimestampGenerator(syncPeriod : TimeSpan.FromSeconds(1), maxAllowedDivergence : TimeSpan.FromMilliseconds(100));
        }

        private static void ResetSystemTimerResolution()
        {
            SetSystemTimerResolution(desiredResolutionMs : -1, setResolution : false);
        }

        private static void SetSystemTimerResolution(double desiredResolutionMs, bool setResolution = true)
        {
            var desiredResolution = (uint)(desiredResolutionMs * ticksPerMs);
            var ntStatus = NtSetTimerResolution(desiredResolution, setResolution, out var currentResolution);
            if (ntStatus != 0)
                throw new InvalidOperationException($"NtSetTimerResolution() failed: 0x{ntStatus:X}");

            Console.Error.WriteLine($"System timer resolution was set to: {(double)currentResolution / ticksPerMs} ms");

            if (setResolution && currentResolution > desiredResolution)
                throw new InvalidOperationException($"Failed to increase system timer resolution to {desiredResolutionMs} ms");
        }

        private static void QuerySystemTimerResolution()
        {
            var ntStatus = NtQueryTimerResolution(out var minResolution, out var maxResolution, out var currentResolution);
            if (ntStatus != 0)
                throw new InvalidOperationException($"NtQueryTimerResolution() failed: 0x{ntStatus:X}");

            Console.Error.WriteLine($"System timer min resolution: {(double)minResolution / ticksPerMs} ms");
            Console.Error.WriteLine($"System timer max resolution: {(double)maxResolution / ticksPerMs} ms");
            Console.Error.WriteLine($"System timer current resolution: {(double)currentResolution / ticksPerMs} ms");
        }

        private const uint ticksPerMs = 10_000;

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern uint NtQueryTimerResolution(out uint minResolution, out uint maxResolution, out uint actualResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern uint NtSetTimerResolution(uint desiredResolution, bool setResolution, out uint currentResolution);

        private static readonly object[] sleepDurations =
            {
                new object[] {TimeSpan.FromMilliseconds(5), 20},
                new object[] {TimeSpan.FromMilliseconds(10), 20},
                new object[] {TimeSpan.FromMilliseconds(20), 20},
                new object[] {TimeSpan.FromMilliseconds(50), 20},
                new object[] {TimeSpan.FromMilliseconds(90), 10},
                new object[] {TimeSpan.FromMilliseconds(99), 10},
                new object[] {TimeSpan.FromMilliseconds(100), 10},
                new object[] {TimeSpan.FromMilliseconds(200), 10},
                new object[] {TimeSpan.FromMilliseconds(500), 10},
                new object[] {TimeSpan.FromMilliseconds(1000), 10},
            };

        private static readonly TimeSpan smallSleepDuration = TimeSpan.FromMilliseconds(23);
        private static readonly bool isCloudCiEnvironment = (Environment.GetEnvironmentVariable("APPVEYOR") ?? string.Empty).Equals("true", StringComparison.InvariantCultureIgnoreCase);
    }
}