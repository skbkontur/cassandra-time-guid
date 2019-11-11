using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using NUnit.Framework;

namespace SkbKontur.Cassandra.TimeBasedUuid.Tests
{
    [TestFixture]
    public class PreciseTimestampGeneratorTest
    {
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
            for (var i = 0; i < iterations; i++)
            {
                var beforeSleep = PreciseTimestampGenerator.Instance.NowTicks();
                SleepWithHihResolution(sleepDuration);
                var afterSleep = PreciseTimestampGenerator.Instance.NowTicks();
                actualDurations[i] = TimeSpan.FromTicks(afterSleep - beforeSleep);
            }
            AssertThatDurationIsEqualTo(actualDurations, sleepDuration);
        }

        [TestCaseSource(nameof(sleepDurations))]
        public void EnsureStopwatchAndWallTimeLikelihood(TimeSpan sleepDuration, int iterations)
        {
            Assert.That(Stopwatch.IsHighResolution);
            var sw = Stopwatch.StartNew();
            var actualDurations = new TimeSpan[iterations];
            for (var i = 0; i < iterations; i++)
            {
                sw.Restart();
                SleepWithHihResolution(sleepDuration);
                sw.Stop();
                actualDurations[i] = sw.Elapsed;
            }
            AssertThatDurationIsEqualTo(actualDurations, sleepDuration);
        }

        [TestCaseSource(nameof(sleepDurations))]
        public void GetDateTimeTicks(TimeSpan sleepDuration, int iterations)
        {
            var actualDurations = new TimeSpan[iterations];
            for (var i = 0; i < iterations; i++)
            {
                var startTimestamp = Stopwatch.GetTimestamp();
                SleepWithHihResolution(sleepDuration);
                var endTimestamp = Stopwatch.GetTimestamp();
                var stopwatchTicks = endTimestamp - startTimestamp;
                actualDurations[i] = TimeSpan.FromTicks(PreciseTimestampGenerator.GetDateTimeTicks(stopwatchTicks));
            }
            AssertThatDurationIsEqualTo(actualDurations, sleepDuration);
        }

        private static void SleepWithHihResolution(TimeSpan sleepDuration)
        {
            var sw = Stopwatch.StartNew();
            var smallSleepDuration = TimeSpan.FromMilliseconds(23);
            if (sleepDuration > smallSleepDuration)
                Thread.Sleep(sleepDuration.Subtract(smallSleepDuration));
            while (sw.Elapsed < sleepDuration)
                Thread.SpinWait(100);
        }

        private static void AssertThatDurationIsEqualTo(TimeSpan[] actualDurations, TimeSpan expectedDuration)
        {
            var epsilon = TimeSpan.FromMilliseconds(10);
            const int errorTolerance = 0;
            Array.Sort(actualDurations);
            Assert.That(actualDurations[0 + errorTolerance], Is.GreaterThan(expectedDuration.Subtract(epsilon)));
            Assert.That(actualDurations[actualDurations.Length - 1 - errorTolerance], Is.LessThan(expectedDuration.Add(epsilon)));
        }

        private static PreciseTimestampGenerator CreateNewTimestampGenerator()
        {
            return new PreciseTimestampGenerator(syncPeriod : TimeSpan.FromSeconds(1), maxAllowedDivergence : TimeSpan.FromMilliseconds(100));
        }

        private static readonly object[] sleepDurations =
            {
                new object[] {TimeSpan.FromMilliseconds(5), 10},
                new object[] {TimeSpan.FromMilliseconds(10), 10},
                new object[] {TimeSpan.FromMilliseconds(20), 10},
                new object[] {TimeSpan.FromMilliseconds(50), 10},
                new object[] {TimeSpan.FromMilliseconds(90), 10},
                new object[] {TimeSpan.FromMilliseconds(99), 10},
                new object[] {TimeSpan.FromMilliseconds(100), 10},
                new object[] {TimeSpan.FromMilliseconds(200), 5},
                new object[] {TimeSpan.FromMilliseconds(500), 5},
                new object[] {TimeSpan.FromMilliseconds(1000), 5},
            };
    }
}