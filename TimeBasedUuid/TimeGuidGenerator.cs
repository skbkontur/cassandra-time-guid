using System;
using System.Security.Cryptography;
using System.Threading;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public class TimeGuidGenerator
    {
        public TimeGuidGenerator([NotNull] PreciseTimestampGenerator preciseTimestampGenerator)
        {
            this.preciseTimestampGenerator = preciseTimestampGenerator;
            rng = new Random(Guid.NewGuid().GetHashCode());
            rngCryptoService = new RNGCryptoServiceProvider();
            defaultNode = GenerateRandomNode();
            defaultClockSequence = GenerateRandomClockSequence();
            countValuesWithOneNode = 0;
        }

        public static Guid MinGuidForTimestamp([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, TimeGuidFormatter.ClockSequenceMinValue, minNode);
        }

        public static Guid MaxGuidForTimestamp([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, TimeGuidFormatter.ClockSequenceMaxValue, maxNode);
        }

        public Guid NewGuid()
        {
            if(Interlocked.Increment(ref countValuesWithOneNode) > maxCountValuesWithOneNode)
            {
                lock(lockObject)
                {
                    if(Interlocked.Increment(ref countValuesWithOneNode) > maxCountValuesWithOneNode)
                    {
                        Interlocked.Exchange(ref defaultNode, GenerateRandomNode());
                        Interlocked.Exchange(ref countValuesWithOneNode, 1);
                    }
                }
            }
            return TimeGuidFormatter.Format(preciseTimestampGenerator.Now(), defaultClockSequence, defaultNode);
        }

        public Guid NewGuid([NotNull] Timestamp timestamp)
        {
            return TimeGuidFormatter.Format(timestamp, GenerateRandomClockSequence(), GenerateRandomNode());
        }

        public Guid NewGuid([NotNull] Timestamp timestamp, ushort clockSequence)
        {
            return TimeGuidFormatter.Format(timestamp, clockSequence, GenerateRandomNode());
        }

        [NotNull]
        private byte[] GenerateRandomNode()
        {
            lock(rngCryptoService)
            {
                var bytes = new byte[6];
                rngCryptoService.GetBytes(bytes);
                return bytes;
            }
        }

        private ushort GenerateRandomClockSequence()
        {
            lock(rng)
                return rng.NextUshort(TimeGuidFormatter.ClockSequenceMinValue, TimeGuidFormatter.ClockSequenceMaxValue);
        }

        private const int maxCountValuesWithOneNode = 1000;
        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
        private readonly Random rng;
        private byte[] defaultNode;
        private readonly ushort defaultClockSequence;
        private readonly RNGCryptoServiceProvider rngCryptoService;
        private int countValuesWithOneNode;
        private readonly object lockObject = new object();
        /*
         * Cassandra TimeUUIDType compares the msb parts as timestamps and the lsb parts as a signed byte array comparison.
         * The min and max possible lsb for a UUID, respectively:
         * 0x8080808080808080L and 0xbf7f7f7f7f7f7f7fL in rfc4122 
         * 0x8080808080808080L and 0xbf7f7f7f7f7f7f7fL in Cassandra (Cassandra ignores the variant field)
         */

        private static readonly byte[] minNode = {0x80, 0x80, 0x80, 0x80, 0x80, 0x80};
        private static readonly byte[] maxNode = {0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f};
        public static readonly Guid MinGuid = TimeGuidFormatter.Format(TimeGuidFormatter.GregorianCalendarStart, TimeGuidFormatter.ClockSequenceMinValue, minNode);
        public static readonly Guid MaxGuid = TimeGuidFormatter.Format(TimeGuidFormatter.GregorianCalendarEnd, TimeGuidFormatter.ClockSequenceMaxValue, maxNode);
    }
}