using System;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    /// <summary>
    ///     Based on https://github.com/fluentcassandra/fluentcassandra/blob/master/src/GuidGenerator.cs
    /// </summary>
    public static class TimeGuidFormatter
    {
        private const int nodeSize = 6;
        private const int nodeOffset = 10;

        private const int clockSequenceOffset = 9;

        // multiplex variant info
        private const int variantOffset = 8;
        private const int variantByteMask = 0x3f;
        private const int variantByteShift = 0x80;

        // multiplex version info
        private const int versionOffset = 7;
        private const int versionByteMask = 0x0f;
        private const int versionByteShift = 4;

        // offset to move from 1/1/0001, which is 0-time for .NET, to gregorian 0-time (1582-10-15 00:00:00Z)
        public static readonly Timestamp GregorianCalendarStart = new Timestamp(new DateTime(1582, 10, 15, 0, 0, 0, DateTimeKind.Utc).Ticks);

        // max timestamp representable by time-based UUID (~5236-03-31 21:21:00Z)
        public static readonly Timestamp GregorianCalendarEnd = new Timestamp(new DateTime(1652084544606846975L, DateTimeKind.Utc).Ticks);

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

        public static byte GetClockSequence(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return bytes[clockSequenceOffset];
        }

        [NotNull]
        public static byte[] GetNode(Guid guid)
        {
            var result = new byte[nodeSize];
            Array.Copy(guid.ToByteArray(), nodeOffset, result, 0, nodeSize);
            return result;
        }

        public static Guid Format([NotNull] Timestamp timestamp, byte clockSequence, [NotNull] byte[] node)
        {
            if (node.Length != nodeSize)
                throw new InvalidOperationException("node must be 6 bytes long");
            if (timestamp < GregorianCalendarStart)
                throw new InvalidOperationException(string.Format("timestamp must not be less than {0}", GregorianCalendarStart));
            if (timestamp > GregorianCalendarEnd)
                throw new InvalidOperationException(string.Format("timestamp must not be greater than {0}", GregorianCalendarEnd));

            var ticks = (timestamp - GregorianCalendarStart).Ticks;
            var ticksBytes = BitConverter.GetBytes(ticks);
            if (ticksBytes.Length != 8)
                throw new InvalidOperationException("ticks must be 8 bytes long");

            var offset = 0;
            var guid = new byte[16];
            BytesToBytes(ticksBytes, guid, ref offset);
            UshortToBytes(clockSequence, guid, ref offset);
            BytesToBytes(node, guid, ref offset);

            guid[variantOffset] &= variantByteMask;
            guid[variantOffset] |= variantByteShift;

            guid[versionOffset] &= versionByteMask;
            guid[versionOffset] |= ((byte)GuidVersion.TimeBased << versionByteShift);

            return new Guid(guid);
        }

        public static void BytesToBytes([NotNull] byte[] field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            Array.Copy(field, 0, targetBuffer, targetBufferOffset, field.Length);
            targetBufferOffset += field.Length;
        }

        public static void UshortToBytes(ushort field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            var endOffset = targetBufferOffset + 1;
            for (var i = 0; i < 2; i++)
            {
                targetBuffer[endOffset - i] = unchecked((byte)(field & 0xff));
                field = (ushort)(field >> 8);
            }
            targetBufferOffset += sizeof(ushort);
        }
    }
}