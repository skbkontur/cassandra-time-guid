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

        public Guid NewGuid()
        {
            return TimeGuidFormatter.Format(preciseTimestampGenerator.Now(), GenerateRandomClockSequence(), GenerateRandomNode());
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
            lock(rng)
                return rng.NextBytes(TimeGuidFormatter.NodeSize);
        }

        private ushort GenerateRandomClockSequence()
        {
            lock(rng)
                return rng.NextUshort(TimeGuidFormatter.MinClockSequence, TimeGuidFormatter.MaxClockSequence + 1);
        }

        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
        private readonly Random rng = new Random(Guid.NewGuid().GetHashCode());
    }
}