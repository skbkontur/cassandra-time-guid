using System;

using JetBrains.Annotations;

using SKBKontur.Catalogue.Objects.Bits;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    // Version 1 UUID layout (https://www.ietf.org/rfc/rfc4122.txt):
    //
    // Most significant long:
    // 0xFFFFFFFF00000000 time_low
    // 0x00000000FFFF0000 time_mid
    // 0x000000000000FF0F time_hi
    // 0x00000000000000F0 version
    //
    // Least significant long:
    // 0xC000000000000000 variant
    // 0x3FFF000000000000 clock_sequence
    // 0x0000FFFFFFFFFFFF node
    //
    // Or in more detail from most significant to least significant byte (octet):
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |0               1               2               3              |
    // |7 6 5 4 3 2 1 0|7 6 5 4 3 2 1 0|7 6 5 4 3 2 1 0|7 6 5 4 3 2 1 0|
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |(msb)                   time_low(0-3)                          |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |         time_mid(0-1)         |  time_hi(0)   |  ver  |t_hi(1)|
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |var| clkseq_hi |  clkseq_low   |           node(0-1)           |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |                           node(2-5)                      (lsb)|
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    //
    // Implementation is based on https://github.com/fluentcassandra/fluentcassandra/blob/master/src/GuidGenerator.cs
    public static class TimeGuidFormatter
    {
        public static Guid Format([NotNull] Timestamp timestamp, ushort clockSequence, [NotNull] byte[] node)
        {
            if(node.Length != NodeSize)
                throw new InvalidProgramStateException("node must be 6 bytes long");
            if(timestamp < GregorianCalendarStart)
                throw new InvalidProgramStateException(string.Format("timestamp must not be less than {0}", GregorianCalendarStart));
            if(timestamp > GregorianCalendarEnd)
                throw new InvalidProgramStateException(string.Format("timestamp must not be greater than {0}", GregorianCalendarEnd));
            if(clockSequence > MaxClockSequence)
                throw new InvalidProgramStateException(string.Format("clockSequence must not be greater than {0}", MaxClockSequence));

            var offset = 0;
            var guid = new byte[BitHelper.GuidSize];

            var timestampTicks = (timestamp - GregorianCalendarStart).Ticks;
            var timestampBytes = EndianBitConverter.Little.GetBytes(timestampTicks);
            for(var i = 0; i < BitHelper.TimestampSize; i++)
                guid[offset++] = timestampBytes[i];

            // xor octets 8-15 with 10000000 for cassandra compatibility as it compares these octets as signed bytes
            var clockSequencebytes = EndianBitConverter.Big.GetBytes(clockSequence);
            for(var i = 0; i < BitHelper.UshortSize; i++)
                guid[offset++] = (byte)(clockSequencebytes[i] ^ signBitMask);
            for(var i = 0; i < NodeSize; i++)
                guid[offset++] = (byte)(node[i] ^ signBitMask);

            // octets[ver_and_timestamp_hi] := 0001xxxx
            guid[versionOffset] &= versionByteMask;
            guid[versionOffset] |= (byte)GuidVersion.TimeBased << versionByteShift;

            // octets[variant_and_clock_sequence] := 10xxxxxx
            guid[variantOffset] &= variantByteMask;
            guid[variantOffset] |= variantBitsValue;

            return new Guid(guid);
        }

        public static GuidVersion GetVersion(Guid guid)
        {
            var guidBytes = guid.ToByteArray();
            return (GuidVersion)(guidBytes[versionOffset] >> versionByteShift);
        }

        [NotNull]
        public static Timestamp GetTimestamp(Guid guid)
        {
            var guidBytes = guid.ToByteArray();

            // octets[ver_and_timestamp_hi] := 0000xxxx
            guidBytes[versionOffset] &= versionByteMask;

            var ticks = EndianBitConverter.Little.ToInt64(guidBytes, 0);
            return new Timestamp(ticks + GregorianCalendarStart.Ticks);
        }

        public static ushort GetClockSequence(Guid guid)
        {
            var bytes = guid.ToByteArray();
            var clockSequenceHighByte = (byte)(bytes[clockSequenceHighByteOffset] ^ signBitMask);
            var clockSequenceLowByte = (byte)(bytes[clockSequenceLowByteOffset] ^ signBitMask);
            return EndianBitConverter.Big.ToUInt16(new[] {clockSequenceHighByte, clockSequenceLowByte}, 0);
        }

        [NotNull]
        public static byte[] GetNode(Guid guid)
        {
            var node = new byte[NodeSize];
            var guidBytes = guid.ToByteArray();
            for(var i = 0; i < NodeSize; i++)
                node[i] = (byte)(guidBytes[nodeOffset + i] ^ signBitMask);
            return node;
        }

        private const int signBitMask = 0x80;

        private const int versionOffset = 7;
        private const byte versionByteMask = 0x0f;
        private const int versionByteShift = 4;

        private const int variantOffset = 8;
        private const byte variantByteMask = 0x3f;
        private const byte variantBitsValue = 0x80;

        private const int clockSequenceHighByteOffset = 8;
        private const int clockSequenceLowByteOffset = 9;

        public const int NodeSize = 6;
        private const int nodeOffset = 10;

        public const ushort MinClockSequence = 0;
        public const ushort MaxClockSequence = 16383; /* = 0x3fff */

        public static readonly byte[] MinNode = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
        public static readonly byte[] MaxNode = {0xff, 0xff, 0xff, 0xff, 0xff, 0xff};

        // min timestamp representable by time-based UUID is gregorian calendar 0-time (1582-10-15 00:00:00Z)
        public static readonly Timestamp GregorianCalendarStart = new Timestamp(new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);

        // max timestamp representable by time-based UUID (~5236-03-31 21:21:00Z)
        public static readonly Timestamp GregorianCalendarEnd = new Timestamp(new DateTime(1652084544606846975L, DateTimeKind.Utc).Ticks);

        public static readonly Guid MinGuid = Format(GregorianCalendarStart, MinClockSequence, MinNode);
        public static readonly Guid MaxGuid = Format(GregorianCalendarEnd, MaxClockSequence, MaxNode);
    }
}