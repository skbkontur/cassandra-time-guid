using System;
using System.Linq;

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
        public static GuidVersion GetVersion(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return (GuidVersion)((bytes[versionOffset] & 0xff) >> versionByteShift); // todo (timeguid): & 0xff - meaningless op?
        }

        [NotNull]
        public static Timestamp GetTimestamp(Guid guid)
        {
            var bytes = guid.ToByteArray();

            // octets[ver_and_timestamp_hi] := 0000xxxx
            bytes[versionOffset] &= (byte)versionByteMask;
            bytes[versionOffset] |= (byte)((byte)GuidVersion.TimeBased >> versionByteShift); // todo (timeguid): meaningless op?

            var ticks = BitConverter.ToInt64(bytes, 0); // todo (timeguid): use EndianBitConverter.Little
            return new Timestamp(ticks + GregorianCalendarStart.Ticks);
        }

        public static ushort GetClockSequence(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return EndianBitConverter.Big.ToUInt16(new[] {(byte)(bytes[clockSequenceHighByteOffset] ^ 0x80), (byte)(bytes[clockSequenceLowByteOffset] ^ 0x80)}, 0);
        }

        [NotNull]
        public static byte[] GetNode(Guid guid)
        {
            var result = new byte[nodeSize];
            Array.Copy(guid.ToByteArray(), nodeOffset, result, 0, nodeSize); // todo (timeguid): why not ^0x80 ?
            return result;
        }

        public static Guid Format([NotNull] Timestamp timestamp, ushort clockSequence, [NotNull] byte[] node)
        {
            if(node.Length != nodeSize)
                throw new InvalidProgramStateException("node must be 6 bytes long");
            if(timestamp < GregorianCalendarStart)
                throw new InvalidProgramStateException(string.Format("timestamp must not be less than {0}", GregorianCalendarStart));
            if(timestamp > GregorianCalendarEnd)
                throw new InvalidProgramStateException(string.Format("timestamp must not be greater than {0}", GregorianCalendarEnd));
            if(clockSequence > MaxClockSequence)
                throw new InvalidProgramStateException(string.Format("clockSequence must not be greater than {0}", MaxClockSequence));

            var ticks = (timestamp - GregorianCalendarStart).Ticks;
            var ticksBytes = EndianBitConverter.Little.GetBytes(ticks);
            if(ticksBytes.Length != 8)
                throw new InvalidProgramStateException("ticks must be 8 bytes long");

            var clockSequencebytes = EndianBitConverter.Big.GetBytes(clockSequence).Select(@byte => (byte)(@byte ^ 0x80)).ToArray(); // todo (timeguid): linq is slow

            var offset = 0;
            var guid = new byte[BitHelper.GuidSize];
            BitHelper.BytesToBytes(ticksBytes, guid, ref offset);
            BitHelper.BytesToBytes(clockSequencebytes, guid, ref offset);
            BitHelper.BytesToBytes(node, guid, ref offset); // todo (timeguid): why not ^0x80 ?

            // octets[ver_and_timestamp_hi] := 0001xxxx
            guid[versionOffset] &= (byte)versionByteMask;
            guid[versionOffset] |= (byte)((byte)GuidVersion.TimeBased << versionByteShift);

            // octets[variant_and_clock_sequence] := 10xxxxxx
            guid[variantOffset] &= (byte)variantByteMask;
            guid[variantOffset] |= (byte)variantByteShift;

            return new Guid(guid);
        }

        private const int versionOffset = 7;
        private const int versionByteMask = 0x0f;
        private const int versionByteShift = 4;

        private const int variantOffset = 8;
        private const int variantByteMask = 0x3f;
        private const int variantByteShift = 0x80;

        private const int clockSequenceHighByteOffset = 8;
        private const int clockSequenceLowByteOffset = 9;

        private const int nodeSize = 6;
        private const int nodeOffset = 10;

        public const ushort MinClockSequence = 0;
        public const ushort MaxClockSequence = 16383;

        // offset to move from 1/1/0001, which is 0-time for .NET, to gregorian 0-time (1582-10-15 00:00:00Z)
        public static readonly Timestamp GregorianCalendarStart = new Timestamp(new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);

        // max timestamp representable by time-based UUID (~5236-03-31 21:21:00Z)
        public static readonly Timestamp GregorianCalendarEnd = new Timestamp(new DateTime(1652084544606846975L, DateTimeKind.Utc).Ticks);
    }
}