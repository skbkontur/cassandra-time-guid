using System;
using System.Threading;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public class TimeGuidGenerator
    {
        public TimeGuidGenerator([NotNull] PreciseTimestampGenerator preciseTimestampGenerator)
        {
            this.preciseTimestampGenerator = preciseTimestampGenerator;
        }

        [NotNull]
        public byte[] NewGuid()
        {
            var nowTimestamp = new Timestamp(preciseTimestampGenerator.NowTicks());
            return TimeGuidBitsLayout.Format(nowTimestamp, GenerateRandomClockSequence(), GenerateRandomNode());
        }

        [NotNull]
        public byte[] NewGuid([NotNull] Timestamp timestamp)
        {
            return TimeGuidBitsLayout.Format(timestamp, GenerateRandomClockSequence(), GenerateRandomNode());
        }

        [NotNull]
        public byte[] NewGuid([NotNull] Timestamp timestamp, ushort clockSequence)
        {
            return TimeGuidBitsLayout.Format(timestamp, clockSequence, GenerateRandomNode());
        }

        [NotNull]
        private byte[] GenerateRandomNode()
        {
            return rng.Value.NextBytes(TimeGuidBitsLayout.NodeSize);
        }

        private ushort GenerateRandomClockSequence()
        {
            return rng.Value.NextUshort(TimeGuidBitsLayout.MinClockSequence, TimeGuidBitsLayout.MaxClockSequence + 1);
        }

        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
        private readonly ThreadLocal<Random> rng = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
    }
}