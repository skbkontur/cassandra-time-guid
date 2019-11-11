using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using NUnit.Framework;

namespace SkbKontur.Cassandra.TimeBasedUuid.Tests
{
    [TestFixture]
    public class TimeGuidBitsLayoutTest
    {
        [Test]
        public void MinGuid()
        {
            Assert.That(TimeGuidBitsLayout.MinTimeGuid, Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80}));
            Assert.That(TimeGuidBitsLayout.GetTimestamp(TimeGuidBitsLayout.MinTimeGuid), Is.EqualTo(new Timestamp(new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc).Ticks)));
            Assert.That(TimeGuidBitsLayout.GetClockSequence(TimeGuidBitsLayout.MinTimeGuid), Is.EqualTo(0));
            Assert.That(TimeGuidBitsLayout.GetNode(TimeGuidBitsLayout.MinTimeGuid), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00}));
        }

        [Test]
        public void MaxGuid()
        {
            Assert.That(TimeGuidBitsLayout.MaxTimeGuid, Is.EqualTo(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x1f, 0xff, 0xbf, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f}));
            Assert.That(TimeGuidBitsLayout.GetTimestamp(TimeGuidBitsLayout.MaxTimeGuid), Is.EqualTo(new Timestamp(new DateTime(1652084544606846975L, DateTimeKind.Utc).Ticks)));
            Assert.That(TimeGuidBitsLayout.GetClockSequence(TimeGuidBitsLayout.MaxTimeGuid), Is.EqualTo(16383));
            Assert.That(TimeGuidBitsLayout.GetNode(TimeGuidBitsLayout.MaxTimeGuid), Is.EqualTo(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff}));
        }

        [Test]
        public void Format_InvalidArgs()
        {
            Assert.Throws<InvalidOperationException>(() => TimeGuidBitsLayout.Format(new Timestamp(TimeGuidBitsLayout.GregorianCalendarStart.Ticks - 1), 0, new byte[6]));
            Assert.Throws<InvalidOperationException>(() => TimeGuidBitsLayout.Format(new Timestamp(TimeGuidBitsLayout.GregorianCalendarEnd.Ticks + 1), 0, new byte[6]));
            Assert.Throws<InvalidOperationException>(() => TimeGuidBitsLayout.Format(Timestamp.Now, TimeGuidBitsLayout.MaxClockSequence + 1, new byte[6]));
            Assert.Throws<InvalidOperationException>(() => TimeGuidBitsLayout.Format(Timestamp.Now, TimeGuidBitsLayout.MaxClockSequence, new byte[4]));
            Assert.Throws<InvalidOperationException>(() => TimeGuidBitsLayout.Format(Timestamp.Now, TimeGuidBitsLayout.MaxClockSequence, new byte[5]));
        }

        [Test]
        public void GetVersion()
        {
            Assert.That(TimeGuidBitsLayout.GetVersion(TimeGuidBitsLayout.Format(new Timestamp(tsGenerator.NowTicks()), RandomClockSequence(), RandomNode())), Is.EqualTo(GuidVersion.TimeBased));
        }

        [Test]
        public void GetVersion_AllDistinctTimestamps()
        {
            var node = RandomNode();
            var clockSequence = RandomClockSequence();
            foreach (var timestamp in AllDistinctTimestamps(TimeSpan.FromHours(10)))
            {
                var timeGuid = TimeGuidBitsLayout.Format(timestamp, clockSequence, node);
                Assert.That(TimeGuidBitsLayout.GetVersion(timeGuid), Is.EqualTo(GuidVersion.TimeBased));
            }
        }

        [Test]
        public void GetTimestamp()
        {
            var timestamp = new Timestamp(tsGenerator.NowTicks());
            var timeGuid = TimeGuidBitsLayout.Format(timestamp, RandomClockSequence(), RandomNode());
            Assert.That(TimeGuidBitsLayout.GetTimestamp(timeGuid), Is.EqualTo(timestamp));
        }

        [Test]
        public void GetTimestamp_GregorianCalendarStart()
        {
            var timestamp = TimeGuidBitsLayout.GregorianCalendarStart;
            var timeGuid = TimeGuidBitsLayout.Format(timestamp, 0, new byte[6]);
            Assert.That(TimeGuidBitsLayout.GetTimestamp(timeGuid), Is.EqualTo(timestamp));
            Assert.That(new Guid(timeGuid), Is.EqualTo(new Guid("00000000-0000-0010-8080-808080808080")));
        }

        [Test]
        public void GetTimestamp_GregorianCalendarEnd()
        {
            var timestamp = TimeGuidBitsLayout.GregorianCalendarEnd;
            var timeGuid = TimeGuidBitsLayout.Format(timestamp, 0, new byte[6]);
            Assert.That(TimeGuidBitsLayout.GetTimestamp(timeGuid), Is.EqualTo(timestamp));
            Assert.That(new Guid(timeGuid), Is.EqualTo(new Guid("ffffffff-ffff-ff1f-8080-808080808080")));
        }

        [Test]
        public void GetTimestamp_AllDistinctTimestamps()
        {
            var node = RandomNode();
            var clockSequence = RandomClockSequence();
            foreach (var timestamp in AllDistinctTimestamps(TimeSpan.FromHours(10)))
            {
                var timeGuid = TimeGuidBitsLayout.Format(timestamp, clockSequence, node);
                Assert.That(TimeGuidBitsLayout.GetTimestamp(timeGuid), Is.EqualTo(timestamp));
            }
        }

        [Test]
        public void GetClockSequence_AllDistinctClockSequences()
        {
            var node = new byte[6];
            var timestamp = Timestamp.Now;
            var uniqueClockSequences = new HashSet<ushort>();
            for (var clockSequence = TimeGuidBitsLayout.MinClockSequence; clockSequence <= TimeGuidBitsLayout.MaxClockSequence; clockSequence++)
            {
                var timeGuid = TimeGuidBitsLayout.Format(timestamp, clockSequence, node);
                var actualClockSequence = TimeGuidBitsLayout.GetClockSequence(timeGuid);
                uniqueClockSequences.Add(actualClockSequence);
                Assert.That(actualClockSequence, Is.EqualTo(clockSequence));
            }
            Assert.That(uniqueClockSequences.Count, Is.EqualTo(TimeGuidBitsLayout.MaxClockSequence + 1));
        }

        [Test]
        public void GetNode()
        {
            var node = RandomNode();
            var timeGuid = TimeGuidBitsLayout.Format(new Timestamp(tsGenerator.NowTicks()), RandomClockSequence(), node);
            Assert.That(TimeGuidBitsLayout.GetNode(timeGuid), Is.EqualTo(node));
        }

        [Test]
        public void IncrementNode()
        {
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00}), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x01}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0xfe}), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0xff}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0xff}), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x01, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0x12, 0x34, 0x56, 0x78, 0xfe, 0xff}), Is.EqualTo(new byte[] {0x12, 0x34, 0x56, 0x78, 0xff, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0x12, 0x34, 0x56, 0x78, 0xff, 0xff}), Is.EqualTo(new byte[] {0x12, 0x34, 0x56, 0x79, 0x00, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0x12, 0xfe, 0xff, 0xff, 0xff, 0xff}), Is.EqualTo(new byte[] {0x12, 0xff, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0x12, 0xff, 0xff, 0xff, 0xff, 0xff}), Is.EqualTo(new byte[] {0x13, 0x00, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0xfe, 0xff, 0xff, 0xff, 0xff, 0xff}), Is.EqualTo(new byte[] {0xff, 0x00, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0xff, 0xfe, 0xff, 0xff, 0xff, 0xff}), Is.EqualTo(new byte[] {0xff, 0xff, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0xff, 0xff, 0xff, 0xff, 0xfe, 0xff}), Is.EqualTo(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0x00}));
            Assert.That(TimeGuidBitsLayout.IncrementNode(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xfe}), Is.EqualTo(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff}));
            Assert.Throws<InvalidOperationException>(() => TimeGuidBitsLayout.IncrementNode(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff}));
        }

        [Test]
        public void DecrementNode()
        {
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x01}), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0xff}), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0xfe}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x01, 0x00}), Is.EqualTo(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0x12, 0x34, 0x56, 0x78, 0xff, 0x00}), Is.EqualTo(new byte[] {0x12, 0x34, 0x56, 0x78, 0xfe, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0x12, 0x34, 0x56, 0x79, 0x00, 0x00}), Is.EqualTo(new byte[] {0x12, 0x34, 0x56, 0x78, 0xff, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0x12, 0xff, 0x00, 0x00, 0x00, 0x00}), Is.EqualTo(new byte[] {0x12, 0xfe, 0xff, 0xff, 0xff, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0x13, 0x00, 0x00, 0x00, 0x00, 0x00}), Is.EqualTo(new byte[] {0x12, 0xff, 0xff, 0xff, 0xff, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0xff, 0x00, 0x00, 0x00, 0x00, 0x00}), Is.EqualTo(new byte[] {0xfe, 0xff, 0xff, 0xff, 0xff, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0xff, 0xff, 0x00, 0x00, 0x00, 0x00}), Is.EqualTo(new byte[] {0xff, 0xfe, 0xff, 0xff, 0xff, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0x00}), Is.EqualTo(new byte[] {0xff, 0xff, 0xff, 0xff, 0xfe, 0xff}));
            Assert.That(TimeGuidBitsLayout.DecrementNode(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff}), Is.EqualTo(new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xfe}));
            Assert.Throws<InvalidOperationException>(() => TimeGuidBitsLayout.DecrementNode(new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00}));
        }

        [Test]
        [Category("LongRunning")]
        public void Perf()
        {
            var timestamp = new Timestamp(tsGenerator.NowTicks());
            var clockSequence = RandomClockSequence();
            var node = RandomNode();
            var sw = Stopwatch.StartNew();
            const int count = 10 * 1000 * 1000;
            for (var i = 0; i < count; i++)
                TimeGuidBitsLayout.Format(timestamp, clockSequence, node);
            sw.Stop();
            Console.Out.WriteLine($"TimeGuidBitsLayout.Format took {sw.ElapsedMilliseconds} ms for {count} calls");
        }

        [Test]
        [Category("LongRunning")]
        public void ExploreBitArithmetic()
        {
            Func<string, string> formatBitsString = s =>
                {
                    var sb = new StringBuilder();
                    if (s.Length < 8)
                        sb.Append(new string('0', 8 - s.Length));
                    sb.Append(s);
                    return sb.ToString();
                };
            for (var i = 0; i <= 0xff; i++)
            {
                var b = (byte)i;
                var nb = (byte)(b ^ 0x80);
                var sb = (sbyte)nb;
                var bitsB = Convert.ToString(b, 2);
                var bitsNb = Convert.ToString(nb, 2);
                var bitsSb = Convert.ToString(sb, 2);
                Console.Out.WriteLine($"{b:D3}: {b:x2}={formatBitsString(bitsB)}, x^0x80: {nb:x2}={formatBitsString(bitsNb)}, sb: {sb:D3}={formatBitsString(bitsSb)}");
            }
        }

        private static byte[] RandomNode()
        {
            return TimeGuidGenerator.GenerateRandomNode();
        }

        private static ushort RandomClockSequence()
        {
            return TimeGuidGenerator.GenerateRandomClockSequence();
        }

        public static IEnumerable<Timestamp> AllDistinctTimestamps(TimeSpan timestampStep)
        {
            yield return TimeGuidBitsLayout.GregorianCalendarStart;
            for (var baseTimestamp = TimeGuidBitsLayout.GregorianCalendarStart; baseTimestamp < TimeGuidBitsLayout.GregorianCalendarEnd - timestampStep; baseTimestamp += timestampStep)
            {
                var randomDelta = TimeSpan.FromTicks((long)(ThreadLocalRandom.Instance.NextDouble() * timestampStep.Ticks));
                yield return baseTimestamp += randomDelta;
            }
            yield return TimeGuidBitsLayout.GregorianCalendarEnd;
        }

        private readonly PreciseTimestampGenerator tsGenerator = new PreciseTimestampGenerator(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100));
    }
}