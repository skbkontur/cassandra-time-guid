using System;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.Bits
{
    public static class BitHelper
    {
        public const int ByteSize = sizeof(byte);
        public const int UintSize = sizeof(uint);
        public const int UshortSize = sizeof(ushort);
        public const int LongSize = sizeof(long);
        public const int UlongSize = sizeof(ulong);
        public const int DateTimeSize = LongSize;
        public const int TimestampSize = LongSize;
        public const int GuidSize = 16;
        public const int TimeGuidSize = 16;

        public static void ByteToBytes(byte field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            targetBuffer[targetBufferOffset] = field;
            targetBufferOffset += ByteSize;
        }

        [NotNull]
        public static byte[] UintToBytes(uint value)
        {
            var offset = 0;
            var buffer = new byte[UintSize];
            UintToBytes(value, buffer, ref offset);
            return buffer;
        }

        public static void UintToBytes(uint field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            EndianBitConverter.Big.CopyBytes(field, targetBuffer, targetBufferOffset);
            targetBufferOffset += UintSize;
        }

        [NotNull]
        public static byte[] UshortToBytes(ushort value)
        {
            var offset = 0;
            var buffer = new byte[UshortSize];
            UshortToBytes(value, buffer, ref offset);
            return buffer;
        }

        public static void UshortToBytes(ushort field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            EndianBitConverter.Big.CopyBytes(field, targetBuffer, targetBufferOffset);
            targetBufferOffset += UshortSize;
        }

        [NotNull]
        public static byte[] LongToBytes(long value)
        {
            var offset = 0;
            var buffer = new byte[LongSize];
            LongToBytes(value, buffer, ref offset);
            return buffer;
        }

        public static void LongToBytes(long field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            EndianBitConverter.Big.CopyBytes(field, targetBuffer, targetBufferOffset);
            targetBufferOffset += LongSize;
        }

        [NotNull]
        public static byte[] UlongToBytes(ulong value)
        {
            var offset = 0;
            var buffer = new byte[UlongSize];
            UlongToBytes(value, buffer, ref offset);
            return buffer;
        }

        public static void UlongToBytes(ulong field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            EndianBitConverter.Big.CopyBytes(field, targetBuffer, targetBufferOffset);
            targetBufferOffset += UlongSize;
        }

        [NotNull]
        public static byte[] DateTimeToBytes(DateTime dateTime)
        {
            return LongToBytes(dateTime.Ticks);
        }

        public static void DateTimeToBytes(DateTime field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            LongToBytes(field.Ticks, targetBuffer, ref targetBufferOffset);
        }

        [NotNull]
        public static byte[] TimestampToBytes([NotNull] Timestamp timestamp)
        {
            return LongToBytes(timestamp.Ticks);
        }

        public static void TimestampToBytes([NotNull] Timestamp field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            LongToBytes(field.Ticks, targetBuffer, ref targetBufferOffset);
        }

        [NotNull]
        public static byte[] TimestampToBytesReverse([NotNull] Timestamp timestamp)
        {
            return LongToBytes(-timestamp.Ticks);
        }

        public static void TimestampToBytesReverse([NotNull] Timestamp field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            LongToBytes(-field.Ticks, targetBuffer, ref targetBufferOffset);
        }

        public static void GuidToBytes(Guid field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            var fieldBytes = field.ToByteArray();
            Array.Copy(fieldBytes, 0, targetBuffer, targetBufferOffset, GuidSize);
            targetBufferOffset += GuidSize;
        }

        public static void BytesToBytes([NotNull] byte[] field, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            BytesToBytes(field, 0, field.Length, targetBuffer, ref targetBufferOffset);
        }

        public static void BytesToBytes([NotNull] byte[] field, int fieldOffset, int fieldLength, [NotNull] byte[] targetBuffer, ref int targetBufferOffset)
        {
            Array.Copy(field, fieldOffset, targetBuffer, targetBufferOffset, fieldLength);
            targetBufferOffset += fieldLength;
        }

        public static byte ReadByte([NotNull] byte[] bytes, ref int offset)
        {
            return bytes[offset++];
        }

        public static uint ReadUint([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadUint(bytes, ref offset);
        }

        public static uint ReadUint([NotNull] byte[] bytes, ref int offset)
        {
            var value = EndianBitConverter.Big.ToUInt32(bytes, offset);
            offset += UintSize;
            return value;
        }

        public static ushort ReadUshort([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadUshort(bytes, ref offset);
        }

        public static ushort ReadUshort([NotNull] byte[] bytes, ref int offset)
        {
            var value = EndianBitConverter.Big.ToUInt16(bytes, offset);
            offset += UshortSize;
            return value;
        }

        public static long ReadLong([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadLong(bytes, ref offset);
        }

        public static long ReadLong([NotNull] byte[] bytes, ref int offset)
        {
            var value = EndianBitConverter.Big.ToInt64(bytes, offset);
            offset += LongSize;
            return value;
        }

        public static ulong ReadUlong([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadUlong(bytes, ref offset);
        }

        public static ulong ReadUlong([NotNull] byte[] bytes, ref int offset)
        {
            var value = EndianBitConverter.Big.ToUInt64(bytes, offset);
            offset += UlongSize;
            return value;
        }

        public static DateTime ReadDateTime([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadDateTime(bytes, ref offset);
        }

        public static DateTime ReadDateTime([NotNull] byte[] bytes, ref int offset)
        {
            var ticks = ReadLong(bytes, ref offset);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        [NotNull]
        public static Timestamp ReadTimestamp([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadTimestamp(bytes, ref offset);
        }

        [NotNull]
        public static Timestamp ReadTimestamp([NotNull] byte[] bytes, ref int offset)
        {
            var ticks = ReadLong(bytes, ref offset);
            return new Timestamp(ticks);
        }

        [NotNull]
        public static Timestamp ReadTimestampReverse([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadTimestampReverse(bytes, ref offset);
        }

        [NotNull]
        public static Timestamp ReadTimestampReverse([NotNull] byte[] bytes, ref int offset)
        {
            var ticks = ReadLong(bytes, ref offset);
            return new Timestamp(-ticks);
        }

        public static Guid ReadGuid([NotNull] byte[] bytes)
        {
            var offset = 0;
            return ReadGuid(bytes, ref offset);
        }

        public static Guid ReadGuid([NotNull] byte[] bytes, ref int offset)
        {
            var a = (int)bytes[offset + 3] << 24 | (int)bytes[offset + 2] << 16 | (int)bytes[offset + 1] << 8 | (int)bytes[offset + 0];
            var b = (short)((int)bytes[offset + 5] << 8 | (int)bytes[offset + 4]);
            var c = (short)((int)bytes[offset + 7] << 8 | (int)bytes[offset + 6]);
            var d = bytes[offset + 8];
            var e = bytes[offset + 9];
            var f = bytes[offset + 10];
            var g = bytes[offset + 11];
            var h = bytes[offset + 12];
            var i = bytes[offset + 13];
            var j = bytes[offset + 14];
            var k = bytes[offset + 15];
            offset += GuidSize;
            return new Guid(a, b, c, d, e, f, g, h, i, j, k);
        }
    }
}