using System;

using NUnit.Framework;

#pragma warning disable 1718
// ReSharper disable UnusedVariable
// ReSharper disable EqualExpressionComparison

namespace SkbKontur.Cassandra.TimeBasedUuid.Tests
{
    [TestFixture]
    public class TimestampTest
    {
        [SetUp]
        public void SetUp()
        {
            var now = DateTime.UtcNow;
            ts1 = new Timestamp(now);
            ts2 = new Timestamp(now);
            tsLess = new Timestamp(now - TimeSpan.FromSeconds(1));
            tsGreater = new Timestamp(now + TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinMaxValues()
        {
            Assert.That(Timestamp.MinValue.Ticks, Is.EqualTo(0));
            Assert.That(Timestamp.MinValue.Ticks, Is.EqualTo(DateTime.MinValue.Ticks));
            Assert.That(Timestamp.MaxValue.Ticks, Is.EqualTo(DateTime.MaxValue.Ticks));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var t = Timestamp.MaxValue + TimeSpan.FromTicks(1);
                });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var t = Timestamp.MinValue - TimeSpan.FromTicks(1);
                });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var t = new Timestamp(-1);
                });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var t = new Timestamp(DateTime.MinValue.Ticks - 1);
                });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var t = new Timestamp(DateTime.MaxValue.Ticks + 1);
                });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var t = new Timestamp(3155378975999999999L + 1);
                });
        }

        [Test]
        public void Now()
        {
            var nowDateTime = DateTime.UtcNow;
            var nowTs = Timestamp.Now;
            Assert.That(nowTs.Ticks - nowDateTime.Ticks, Is.LessThan(100 * 10000));
            Assert.That((nowTs.ToDateTime() - nowDateTime).TotalMilliseconds, Is.LessThan(100));
            Assert.That((nowTs.ToDateTimeOffset() - new DateTimeOffset(nowDateTime)).TotalMilliseconds, Is.LessThan(100));
        }

        [Test]
        public void UtcTicks()
        {
            var now = DateTime.Now;
            var ts = new Timestamp(now);
            Assert.That(ts.Ticks, Is.EqualTo(now.ToUniversalTime().Ticks));
        }

        [Test]
        public void Equals()
        {
            Assert.That(ts1.Equals(null), Is.False);
            Assert.That(ts1.Equals(ts1), Is.True);
            Assert.That(ts1.Equals(ts2), Is.True);
            Assert.That(ts1.Equals(tsGreater), Is.False);
        }

        [Test]
        public void EqualsOp()
        {
            Assert.That(ts1 == null, Is.False);
            Assert.That(null == ts1, Is.False);
            Assert.That(ts1 == ts1, Is.True);
            Assert.That(ts1 == ts2, Is.True);
            Assert.That(ts1 == tsGreater, Is.False);
        }

        [Test]
        public void NotEqualsOp()
        {
            Assert.That(ts1 != null, Is.True);
            Assert.That(null != ts1, Is.True);
            Assert.That(ts1 != ts1, Is.False);
            Assert.That(ts1 != ts2, Is.False);
            Assert.That(ts1 != tsGreater, Is.True);
        }

        [Test]
        public void CompareTo()
        {
            Assert.That(ts1.CompareTo(null), Is.GreaterThan(0));
            Assert.That(ts1.CompareTo(ts1), Is.EqualTo(0));
            Assert.That(ts1.CompareTo(ts2), Is.EqualTo(0));
            Assert.That(ts1.CompareTo(tsLess), Is.GreaterThan(0));
            Assert.That(ts1.CompareTo(tsGreater), Is.LessThan(0));
        }

        [Test]
        public void LessOp()
        {
            Assert.That(ts1 < null, Is.False);
            Assert.That(null < ts1, Is.True);
            Assert.That(ts1 < ts1, Is.False);
            Assert.That(ts1 < ts2, Is.False);
            Assert.That(ts1 < tsLess, Is.False);
            Assert.That(ts1 < tsGreater, Is.True);
        }

        [Test]
        public void LessOrEqualOp()
        {
            Assert.That(ts1 <= null, Is.False);
            Assert.That(null <= ts1, Is.True);
            Assert.That(ts1 <= ts1, Is.True);
            Assert.That(ts1 <= ts2, Is.True);
            Assert.That(ts1 <= tsLess, Is.False);
            Assert.That(ts1 <= tsGreater, Is.True);
        }

        [Test]
        public void GreaterOp()
        {
            Assert.That(ts1 > null, Is.True);
            Assert.That(null > ts1, Is.False);
            Assert.That(ts1 > ts1, Is.False);
            Assert.That(ts1 > ts2, Is.False);
            Assert.That(ts1 > tsLess, Is.True);
            Assert.That(ts1 > tsGreater, Is.False);
        }

        [Test]
        public void GreaterOrEqualOp()
        {
            Assert.That(ts1 >= null, Is.True);
            Assert.That(null >= ts1, Is.False);
            Assert.That(ts1 >= ts1, Is.True);
            Assert.That(ts1 >= ts2, Is.True);
            Assert.That(ts1 >= tsLess, Is.True);
            Assert.That(ts1 >= tsGreater, Is.False);
        }

        [Test]
        public void AddOp()
        {
            var positiveTimeSpan = TimeSpan.FromSeconds(1);
            Assert.That(ts1 + positiveTimeSpan, Is.EqualTo(new Timestamp(ts1.ToDateTime() + positiveTimeSpan)));
            var negativeTimeSpan = TimeSpan.FromSeconds(-1);
            Assert.That(ts1 + negativeTimeSpan, Is.EqualTo(new Timestamp(ts1.ToDateTime() + negativeTimeSpan)));
        }

        [Test]
        public void SubtractOp()
        {
            var positiveTimeSpan = TimeSpan.FromSeconds(1);
            Assert.That(ts1 - positiveTimeSpan, Is.EqualTo(new Timestamp(ts1.ToDateTime() - positiveTimeSpan)));
            var negativeTimeSpan = TimeSpan.FromSeconds(-1);
            Assert.That(ts1 - negativeTimeSpan, Is.EqualTo(new Timestamp(ts1.ToDateTime() - negativeTimeSpan)));
        }

        private Timestamp ts1;
        private Timestamp ts2;
        private Timestamp tsLess;
        private Timestamp tsGreater;
    }
}

// ReSharper restore EqualExpressionComparison
// ReSharper restore UnusedVariable
#pragma warning restore 1718