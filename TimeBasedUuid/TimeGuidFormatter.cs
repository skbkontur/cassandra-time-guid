using System;

using JetBrains.Annotations;

using SKBKontur.Catalogue.Objects.BitConversion;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    /// <summary>
    ///     Based on https://github.com/fluentcassandra/fluentcassandra/blob/master/src/GuidGenerator.cs
    /// </summary>
    public static class TimeGuidFormatter
    {
        public static GuidVersion GetVersion(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return (GuidVersion)((bytes[versionOffset] & 0xff) >> versionByteShift);
        }

        [NotNull]
        public static Timestamp GetTimestamp(Guid guid)
        {
            var bytes = guid.ToByteArray();

            // reverse the version
            bytes[versionOffset] &= versionByteMask;
            bytes[versionOffset] |= ((byte)GuidVersion.TimeBased >> versionByteShift);

            var ticks = BitConverter.ToInt64(bytes, 0);
            return new Timestamp(ticks + GregorianCalendarStart.Ticks);
        }

        public static ClockSequence GetClockSequence(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return new ClockSequence(new[] {bytes[clockSequenceHighByteOffset], bytes[clockSequenceLowByteOffset]});
        }

        [NotNull]
        public static byte[] GetNode(Guid guid)
        {
            var result = new byte[nodeSize];
            Array.Copy(guid.ToByteArray(), nodeOffset, result, 0, nodeSize);
            return result;
        }

        public static Guid Format([NotNull] Timestamp timestamp, ClockSequence clockSequence, [NotNull] byte[] node)
        {
            if(node.Length != nodeSize)
                throw new InvalidOperationException("node must be 6 bytes long");
            if(timestamp < GregorianCalendarStart)
                throw new InvalidOperationException(string.Format("timestamp must not be less than {0}", GregorianCalendarStart));
            if(timestamp > GregorianCalendarEnd)
                throw new InvalidOperationException(string.Format("timestamp must not be greater than {0}", GregorianCalendarEnd));

            var ticks = (timestamp - GregorianCalendarStart).Ticks;
            var ticksBytes = EndianBitConverter.Little.GetBytes(ticks);
            if(ticksBytes.Length != 8)
                throw new InvalidOperationException("ticks must be 8 bytes long");

            var offset = 0;
            var guid = new byte[BitHelper.GuidSize];
            BitHelper.BytesToBytes(ticksBytes, guid, ref offset);
            BitHelper.BytesToBytes(clockSequence.GetBytes(), guid, ref offset);
            BitHelper.BytesToBytes(node, guid, ref offset);

            guid[variantOffset] |= variantByteShift;

            guid[versionOffset] &= versionByteMask;
            guid[versionOffset] |= ((byte)GuidVersion.TimeBased << versionByteShift);

            return new Guid(guid);
        }

        private const int clockSequenceHighByteOffset = 8;
        private const int clockSequenceLowByteOffset = 9;
        private const int nodeSize = 6;
        private const int nodeOffset = 10;
        // multiplex variant info
        private const int variantOffset = 8;
        private const int variantByteShift = 0x80;
        // multiplex version info
        private const int versionOffset = 7;
        private const int versionByteMask = 0x0f;
        private const int versionByteShift = 4;
        // offset to move from 1/1/0001, which is 0-time for .NET, to gregorian 0-time (1582-10-15 00:00:00Z)
        public static readonly Timestamp GregorianCalendarStart = new Timestamp(new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);
        // max timestamp representable by time-based UUID (~5236-03-31 21:21:00Z)
        public static readonly Timestamp GregorianCalendarEnd = new Timestamp(new DateTime(1652084544606846975L, DateTimeKind.Utc).Ticks);
    }
}