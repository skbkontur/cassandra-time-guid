using System;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public class TimeGuidGenerator
    {
        /*
         * Cassandra TimeUUIDType compares the msb parts as timestamps and the lsb parts as a signed byte array comparison.
         * The min and max possible lsb for a UUID, respectively:
         * 0x8080808080808080L and 0xbf7f7f7f7f7f7f7fL in rfc4122 
         * 0x8080808080808080L and 0x7f7f7f7f7f7f7f7fL in Cassandra (Cassandra ignores the variant field)
         * 0x8080808080808080L and 0x807f7f7f7f7f7f7fL in our code (clock_seq_hi_and_reserved is always 0x80, we use only clock_seq_low)
         */
        private const byte minClockSequence = 0x80;
        private static readonly byte[] minNode = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, };
        private const byte maxClockSequence = 0x7f;
        private static readonly byte[] maxNode = { 0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f, };

        public static readonly Guid MinGuid = TimeGuidFormatter.Format(TimeGuidFormatter.GregorianCalendarStart, minClockSequence, minNode);
        public static readonly Guid MaxGuid = TimeGuidFormatter.Format(TimeGuidFormatter.GregorianCalendarEnd, maxClockSequence, maxNode);

        public static Guid MinGuidForTimestamp([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, minClockSequence, minNode);
        }

        public static Guid MaxGuidForTimestamp([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, maxClockSequence, maxNode);
        }


        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
        private readonly Random rng;
        private readonly byte[] defaultNode;
        private readonly byte defaultClockSequence;

        public TimeGuidGenerator([NotNull] PreciseTimestampGenerator preciseTimestampGenerator)
        {
            this.preciseTimestampGenerator = preciseTimestampGenerator;
            rng = new Random();
            defaultNode = GenerateRandomNode();
            defaultClockSequence = GenerateRandomClockSequence();
        }

        public Guid NewGuid()
        {
            return TimeGuidFormatter.Format(preciseTimestampGenerator.Now(), defaultClockSequence, defaultNode);
        }

        public Guid NewGuid([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, GenerateRandomClockSequence(), GenerateRandomNode());
        }

        public Guid NewGuid([NotNull] Timestamp timestamp, byte clockSequence)
        {
            return TimeGuidFormatter.Format(timestamp, clockSequence, GenerateRandomNode());
        }

        [NotNull]
        private byte[] GenerateRandomNode()
        {
            lock (rng)
                return rng.NextBytes(6);
        }

        private byte GenerateRandomClockSequence()
        {
            lock (rng)
                return rng.NextByte();
        }
    }
}