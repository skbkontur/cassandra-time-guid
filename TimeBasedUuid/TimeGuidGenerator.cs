using System;

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
            return TimeGuidBitsLayout.Format(preciseTimestampGenerator.Now(), GenerateRandomClockSequence(), GenerateRandomNode());
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
            lock(rng)
                return rng.NextBytes(TimeGuidBitsLayout.NodeSize);
        }

        private ushort GenerateRandomClockSequence()
        {
            lock(rng)
                return rng.NextUshort(TimeGuidBitsLayout.MinClockSequence, TimeGuidBitsLayout.MaxClockSequence + 1);
        }

        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
        private readonly Random rng = new Random(Guid.NewGuid().GetHashCode());
    }
}