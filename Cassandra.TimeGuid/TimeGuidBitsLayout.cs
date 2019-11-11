using System;

using JetBrains.Annotations;

using SkbKontur.Cassandra.TimeGuid.Bits;

namespace SkbKontur.Cassandra.TimeGuid
{
    // Version 1 UUID layout (https://www.ietf.org/rfc/rfc4122.txt):
    //
    // Most significant long:
    // 0xFFFFFFFF00000000 time_low
    // 0x00000000FFFF0000 time_mid
    // 0x000000000000F000 version
    // 0x0000000000000FFF time_hi
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
    // |(msb)                      time_low                            |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |           time_mid            |  ver  |       time_hi         |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |var| clkseq_hi |  clkseq_low   |           node(0-1)           |
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // |                           node(2-5)                      (lsb)|
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    //
    // Implementation is based on https://github.com/fluentcassandra/fluentcassandra/blob/master/src/GuidGenerator.cs
    public static class TimeGuidBitsLayout
    {
        [NotNull]
        public static byte[] Format([NotNull] Timestamp timestamp, ushort clockSequence, [NotNull] byte[] node)
        {
            if (node.Length != NodeSize)
                throw new InvalidOperationException(string.Format("node must be {0} bytes long", NodeSize));
            if (timestamp < GregorianCalendarStart)
                throw new InvalidOperationException(string.Format("timestamp must not be less than {0}", GregorianCalendarStart));
            if (timestamp > GregorianCalendarEnd)
                throw new InvalidOperationException(string.Format("timestamp must not be greater than {0}", GregorianCalendarEnd));
            if (clockSequence > MaxClockSequence)
                throw new InvalidOperationException(string.Format("clockSequence must not be greater than {0}", MaxClockSequence));

            var timestampTicks = (timestamp - GregorianCalendarStart).Ticks;
            var timestampBytes = EndianBitConverter.Little.GetBytes(timestampTicks);
            var clockSequencebytes = EndianBitConverter.Big.GetBytes(clockSequence);

            var bytes = new byte[BitHelper.TimeGuidSize];
            bytes[0] = timestampBytes[3];
            bytes[1] = timestampBytes[2];
            bytes[2] = timestampBytes[1];
            bytes[3] = timestampBytes[0];
            bytes[4] = timestampBytes[5];
            bytes[5] = timestampBytes[4];
            bytes[6] = timestampBytes[7];
            bytes[7] = timestampBytes[6];

            // xor octets 8-15 with 10000000 for cassandra compatibility as it compares these octets as signed bytes
            var offset = 8;
            for (var i = 0; i < BitHelper.UshortSize; i++)
                bytes[offset++] = (byte)(clockSequencebytes[i] ^ signBitMask);
            for (var i = 0; i < NodeSize; i++)
                bytes[offset++] = (byte)(node[i] ^ signBitMask);

            // octets[ver_and_timestamp_hi] := 0001xxxx
            bytes[versionOffset] &= versionByteMask;
            bytes[versionOffset] |= (byte)GuidVersion.TimeBased << versionByteShift;

            // octets[variant_and_clock_sequence] := 10xxxxxx
            bytes[variantOffset] &= variantByteMask;
            bytes[variantOffset] |= variantBitsValue;

            return bytes;
        }

        public static GuidVersion GetVersion([NotNull] byte[] bytes)
        {
            if (bytes.Length != BitHelper.TimeGuidSize)
                throw new InvalidOperationException(string.Format("bytes must be {0} bytes long", BitHelper.TimeGuidSize));
            return (GuidVersion)(bytes[versionOffset] >> versionByteShift);
        }

        [NotNull]
        public static Timestamp GetTimestamp([NotNull] byte[] bytes)
        {
            if (bytes.Length != BitHelper.TimeGuidSize)
                throw new InvalidOperationException(string.Format("bytes must be {0} bytes long", BitHelper.TimeGuidSize));

            var timestampBytes = new byte[BitHelper.TimestampSize];
            timestampBytes[0] = bytes[3];
            timestampBytes[1] = bytes[2];
            timestampBytes[2] = bytes[1];
            timestampBytes[3] = bytes[0];
            timestampBytes[4] = bytes[5];
            timestampBytes[5] = bytes[4];
            timestampBytes[6] = bytes[7];
            timestampBytes[7] = bytes[6];

            // octets[ver_and_timestamp_hi] := 0000xxxx
            timestampBytes[timestampBytes.Length - 1] &= versionByteMask;

            var ticks = EndianBitConverter.Little.ToInt64(timestampBytes, 0);
            return new Timestamp(ticks + GregorianCalendarStart.Ticks);
        }

        public static ushort GetClockSequence([NotNull] byte[] bytes)
        {
            if (bytes.Length != BitHelper.TimeGuidSize)
                throw new InvalidOperationException(string.Format("bytes must be {0} bytes long", BitHelper.TimeGuidSize));
            var clockSequenceHighByte = (byte)(bytes[clockSequenceHighByteOffset] ^ signBitMask);
            var clockSequenceLowByte = (byte)(bytes[clockSequenceLowByteOffset] ^ signBitMask);
            return EndianBitConverter.Big.ToUInt16(new[] {clockSequenceHighByte, clockSequenceLowByte}, 0);
        }

        [NotNull]
        public static byte[] GetNode([NotNull] byte[] bytes)
        {
            if (bytes.Length != BitHelper.TimeGuidSize)
                throw new InvalidOperationException(string.Format("bytes must be {0} bytes long", BitHelper.TimeGuidSize));
            var node = new byte[NodeSize];
            for (var i = 0; i < NodeSize; i++)
                node[i] = (byte)(bytes[nodeOffset + i] ^ signBitMask);
            return node;
        }

        [NotNull]
        public static byte[] IncrementNode([NotNull] byte[] nodeBytes)
        {
            if (nodeBytes.Length != NodeSize)
                throw new InvalidOperationException(string.Format("nodeBytes must be {0} bytes long", NodeSize));
            var carry = true;
            var node = new byte[NodeSize];
            for (var i = NodeSize - 1; i >= 0; i--)
            {
                var currentDigit = carry ? nodeBytes[i] + 1 : nodeBytes[i];
                if (currentDigit > byte.MaxValue)
                {
                    node[i] = 0;
                    carry = true;
                }
                else
                {
                    node[i] = (byte)currentDigit;
                    carry = false;
                }
            }
            if (carry)
                throw new InvalidOperationException("Cannot increment MaxNode");
            return node;
        }

        [NotNull]
        public static byte[] DecrementNode([NotNull] byte[] nodeBytes)
        {
            if (nodeBytes.Length != NodeSize)
                throw new InvalidOperationException(string.Format("nodeBytes must be {0} bytes long", NodeSize));
            var carry = true;
            var node = new byte[NodeSize];
            for (var i = NodeSize - 1; i >= 0; i--)
            {
                var currentDigit = carry ? nodeBytes[i] - 1 : nodeBytes[i];
                if (currentDigit < 0)
                {
                    node[i] = byte.MaxValue;
                    carry = true;
                }
                else
                {
                    node[i] = (byte)currentDigit;
                    carry = false;
                }
            }
            if (carry)
                throw new InvalidOperationException("Cannot decrement MinNode");
            return node;
        }

        private const int signBitMask = 0x80;

        private const int versionOffset = 6;
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

        public static readonly byte[] MinTimeGuid = Format(GregorianCalendarStart, MinClockSequence, MinNode);
        public static readonly byte[] MaxTimeGuid = Format(GregorianCalendarEnd, MaxClockSequence, MaxNode);
    }
}