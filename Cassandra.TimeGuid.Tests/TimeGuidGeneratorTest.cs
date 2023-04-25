using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

using NUnit.Framework;

using SkbKontur.Cassandra.TimeBasedUuid.Bits;

namespace SkbKontur.Cassandra.TimeBasedUuid.Tests
{
    [TestFixture]
    public class TimeGuidGeneratorTest
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
        public void TimeGuidGenerator_Perf(int threadsCount)
        {
            const int totalIterationsCount = 64 * 1000 * 1000;
            var sut = new TimeGuidGenerator(CreateNewTimestampGenerator());
            PerfMeasurement.Do("TimeGuidGenerator.NewGuid()", threadsCount, totalIterationsCount, () => sut.NewGuid());
        }

        [Test]
        public void TimeGuidGenerator_Collisions()
        {
            var guidGen = new TimeGuidGenerator(CreateNewTimestampGenerator());
            var results = new Dictionary<byte[], byte>(10 * 1000 * 1000, ByteArrayComparer.Instance);
            for (var i = 0; i < 10 * 1000 * 1000; i++)
                results.Add(guidGen.NewGuid(), 0);
        }

        [Test]
        public void TimeGuidGenerator_Collisions_MultiProc()
        {
            const int count = 100 * 1000;
            const int threadsCount = 50;
            var lists = new List<List<byte[]>>();
            var threads = new List<Thread>();
            var startSignal = new ManualResetEvent(false);
            for (var i = 0; i < threadsCount; i++)
            {
                var list = new List<byte[]>();
                lists.Add(list);
                var thread = new Thread(() =>
                    {
                        startSignal.WaitOne();
                        var guidGen = new TimeGuidGenerator(CreateNewTimestampGenerator());
                        for (var i1 = 0; i1 < count; i1++)
                            list.Add(guidGen.NewGuid());
                    });
                thread.Start();
                threads.Add(thread);
            }
            startSignal.Set();
            threads.ForEach(thread => thread.Join());
            Assert.That(lists.SelectMany(list => list).ToArray().Distinct(ByteArrayComparer.Instance).Count(), Is.EqualTo(threadsCount * count));
        }

        [Test]
        public void GenerateByTimestamp()
        {
            var guidGen = new TimeGuidGenerator(PreciseTimestampGenerator.Instance);
            var ts = Timestamp.Now;
            var guid = guidGen.NewGuid(ts);
            Assert.That(TimeGuidBitsLayout.GetTimestamp(guid), Is.EqualTo(ts));
        }

        [Test]
        public void GenerateByTimestamp_InvalidTimestamp()
        {
            var guidGen = new TimeGuidGenerator(PreciseTimestampGenerator.Instance);
            Assert.Throws<InvalidOperationException>(() => guidGen.NewGuid(Timestamp.MinValue));
            Assert.Throws<InvalidOperationException>(() => guidGen.NewGuid(Timestamp.MaxValue));
        }

        [Test]
        public void GenerateByTimestampAndClockSequence()
        {
            var guidGen = new TimeGuidGenerator(PreciseTimestampGenerator.Instance);
            var ts = Timestamp.Now;
            var clockSequence = TimeGuidGenerator.GenerateRandomClockSequence();
            var guid = guidGen.NewGuid(ts, clockSequence);
            Assert.That(TimeGuidBitsLayout.GetTimestamp(guid), Is.EqualTo(ts));
            Assert.That(TimeGuidBitsLayout.GetClockSequence(guid), Is.EqualTo(clockSequence));
        }

        [Test]
        public void GenerateByTimestampAndClockSequence_InvalidClockSequence()
        {
            var guidGen = new TimeGuidGenerator(PreciseTimestampGenerator.Instance);
            Assert.Throws<InvalidOperationException>(() => guidGen.NewGuid(Timestamp.Now, ushort.MaxValue));
            Assert.Throws<InvalidOperationException>(() => guidGen.NewGuid(Timestamp.Now, TimeGuidBitsLayout.MaxClockSequence + 1));
        }

        [Test]
        [Category("LongRunning")]
        public void RngTestPerf()
        {
            DoRngPerfTest("GenerateRandomNode()", GenerateRandomNode);
            DoRngPerfTest("GenerateRandomNodeCrypto()", GenerateRandomNodeCrypto);
        }

        private static void DoRngPerfTest(string actionName, Func<byte[]> generateRandomNode)
        {
            var bytesGenerated = 0L;
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 1 * 1000 * 1000; i++)
                bytesGenerated += generateRandomNode().Length;
            sw.Stop();
            Console.Out.WriteLine($"{actionName} took {sw.ElapsedMilliseconds} ms to generate {bytesGenerated} bytes");
        }

        private static byte[] GenerateRandomNode()
        {
            return TimeGuidGenerator.GenerateRandomNode();
        }

        private byte[] GenerateRandomNodeCrypto()
        {
            var bytes = new byte[6];
            randomNumberGenerator.GetBytes(bytes);
            return bytes;
        }

        private static PreciseTimestampGenerator CreateNewTimestampGenerator()
        {
            return new PreciseTimestampGenerator(syncPeriod : TimeSpan.FromSeconds(1), maxAllowedDivergence : TimeSpan.FromMilliseconds(100));
        }

        private readonly RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
    }
}