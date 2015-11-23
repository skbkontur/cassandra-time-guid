using System;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public class TimeGuidGenerator
    {
        public TimeGuidGenerator([NotNull] PreciseTimestampGenerator preciseTimestampGenerator)
        {
            this.preciseTimestampGenerator = preciseTimestampGenerator;
            rng = new Random();
            defaultNode = GenerateRandomNode();
            defaultClockSequence = GenerateRandomClockSequence();
        }

        public static Guid MinGuidForTimestamp([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, ClockSequence.MinValue, minNode);
        }

        public static Guid MaxGuidForTimestamp([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, ClockSequence.MaxValue, maxNode);
        }

        public Guid NewGuid()
        {
            return TimeGuidFormatter.Format(preciseTimestampGenerator.Now(), defaultClockSequence, defaultNode);
        }

        public Guid NewGuid([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, GenerateRandomClockSequence(), GenerateRandomNode());
        }

        public Guid NewGuid([NotNull] Timestamp timestamp, ClockSequence clockSequence)
        {
            return TimeGuidFormatter.Format(timestamp, clockSequence, GenerateRandomNode());
        }

        [NotNull]
        private byte[] GenerateRandomNode()
        {
            lock(rng)
                return rng.NextBytes(6);
        }

        private ClockSequence GenerateRandomClockSequence()
        {
            lock(rng)
                return new ClockSequence(rng.NextBytes(2));
        }

        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
        private readonly Random rng;
        private readonly byte[] defaultNode;
        private readonly ClockSequence defaultClockSequence;
        /*
         * Cassandra TimeUUIDType compares the msb parts as timestamps and the lsb parts as a signed byte array comparison.
         * The min and max possible lsb for a UUID, respectively:
         * 0x8080808080808080L and 0xbf7f7f7f7f7f7f7fL in rfc4122 
         * 0x8080808080808080L and 0xbf7f7f7f7f7f7f7fL in Cassandra (Cassandra ignores the variant field)
         */
        private static readonly byte[] minNode = {0x80, 0x80, 0x80, 0x80, 0x80, 0x80,};
        private static readonly byte[] maxNode = {0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f,};
        public static readonly Guid MinGuid = TimeGuidFormatter.Format(TimeGuidFormatter.GregorianCalendarStart, ClockSequence.MinValue, minNode);
        public static readonly Guid MaxGuid = TimeGuidFormatter.Format(TimeGuidFormatter.GregorianCalendarEnd, ClockSequence.MaxValue, maxNode);
    }
}