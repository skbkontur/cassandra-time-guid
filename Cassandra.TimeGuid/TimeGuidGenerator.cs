using JetBrains.Annotations;

namespace SkbKontur.Cassandra.TimeBasedUuid
{
    internal class TimeGuidGenerator
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
        public static byte[] GenerateRandomNode()
        {
            var buffer = new byte[TimeGuidBitsLayout.NodeSize];
            ThreadLocalRandom.Instance.NextBytes(buffer);
            return buffer;
        }

        public static ushort GenerateRandomClockSequence()
        {
            return (ushort)ThreadLocalRandom.Instance.Next(TimeGuidBitsLayout.MinClockSequence, TimeGuidBitsLayout.MaxClockSequence + 1);
        }

        private readonly PreciseTimestampGenerator preciseTimestampGenerator;
    }
}