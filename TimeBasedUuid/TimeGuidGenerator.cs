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
                return rng.NextUshort(TimeGuidFormatter.MinClockSequence, TimeGuidFormatter.MaxClockSequence);
        }

        private const int maxCountValuesWithOneNode = 1000;
        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
        private readonly Random rng;
        private byte[] defaultNode;
        private readonly ushort defaultClockSequence;
        private readonly RNGCryptoServiceProvider rngCryptoService;
        private int countValuesWithOneNode;
        private readonly object lockObject = new object();
    }
}