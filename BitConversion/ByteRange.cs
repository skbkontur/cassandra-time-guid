using System;
using System.IO;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.BitConversion
{
    public sealed class ByteRange : IEquatable<ByteRange>, IComparable<ByteRange>
    {
        private readonly byte[] bytes;
        private readonly int offset;
        private readonly int length;

        public ByteRange([NotNull] byte[] bytes)
            : this(bytes, 0, bytes.Length)
        {
        }

        public ByteRange([NotNull] byte[] bytes, int offset)
            : this(bytes, offset, bytes.Length - offset)
        {
        }

        public ByteRange([NotNull] byte[] bytes, int offset, int length)
        {
            if (offset < 0 || length < 0 || offset + length > bytes.Length)
                throw new InvalidOperationException(string.Format("Invalid args: bytes=byte[{0}], offset={1}, length={2}", bytes.Length, offset, length));
            this.bytes = bytes;
            this.offset = offset;
            this.length = length;
        }

        [NotNull]
        public byte[] DangerousGetBytes()
        {
            return bytes;
        }

        public int Offset
        {
            get { return offset; }
        }

        public int Length
        {
            get { return length; }
        }

        [NotNull]
        public byte[] ToByteArray()
        {
            return offset == 0 && length == bytes.Length ? bytes : CopyToByteArray();
        }

        [NotNull]
        public byte[] CopyToByteArray()
        {
            var result = new byte[length];
            CopyTo(0, result, 0, length);
            return result;
        }

        [NotNull]
        public ByteRange GetSubRange(int subRangeOffset, int subRangeLength)
        {
            if (subRangeOffset < 0 || subRangeLength < 0 || subRangeOffset + subRangeLength > length)
                throw new InvalidOperationException(string.Format("Invalid args: subRangeOffset = {0}, subRangeLength = {1}, this = {2}", subRangeOffset, subRangeLength, this));
            return new ByteRange(bytes, offset + subRangeOffset, subRangeLength);
        }

        public void CopyTo(int sourceOffset, [NotNull] byte[] target, int targetOffset, int bytesToCopy)
        {
            if (sourceOffset < 0 || bytesToCopy < 0 || sourceOffset + bytesToCopy > length)
                throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, bytesToCopy = {1}, this = {2}", sourceOffset, bytesToCopy, this));
            Array.Copy(bytes, offset + sourceOffset, target, targetOffset, bytesToCopy);
        }

        public Guid ReadGuid(ref int sourceOffset)
        {
            if (sourceOffset < 0 || sourceOffset + BitHelper.GuidSize > length)
                throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, this = {1}", sourceOffset, this));
            var tmp = offset + sourceOffset;
            var value = BitHelper.ReadGuid(bytes, ref tmp);
            sourceOffset += BitHelper.GuidSize;
            return value;
        }

        public byte ReadByte(ref int sourceOffset)
        {
            if (sourceOffset < 0 || sourceOffset + BitHelper.ByteSize > length)
                throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, this = {1}", sourceOffset, this));
            var tmp = offset + sourceOffset;
            var value = BitHelper.ReadByte(bytes, ref tmp);
            sourceOffset += BitHelper.ByteSize;
            return value;
        }

        public byte this[int i]
        {
            get
            {
                if (i < 0 || i + 1 > length)
                    throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, this = {1}", i, this));
                return bytes[offset + i];
            }
        }

        public DateTime ReadDateTime(ref int sourceOffset)
        {
            if (sourceOffset < 0 || sourceOffset + BitHelper.DateTimeSize > length)
                throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, this = {1}", sourceOffset, this));
            var tmp = offset + sourceOffset;
            var value = BitHelper.ReadDateTime(bytes, ref tmp);
            sourceOffset += BitHelper.DateTimeSize;
            return value;
        }

        public ushort ReadUshort(ref int sourceOffset)
        {
            if (sourceOffset < 0 || sourceOffset + BitHelper.UshortSize > length)
                throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, this = {1}", sourceOffset, this));
            var tmp = offset + sourceOffset;
            var value = BitHelper.ReadUshort(bytes, ref tmp);
            sourceOffset += BitHelper.UshortSize;
            return value;
        }

        public uint ReadUint(ref int sourceOffset)
        {
            if (sourceOffset < 0 || sourceOffset + BitHelper.UintSize > length)
                throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, this = {1}", sourceOffset, this));
            var tmp = offset + sourceOffset;
            var value = BitHelper.ReadUint(bytes, ref tmp);
            sourceOffset += BitHelper.UintSize;
            return value;
        }

        [NotNull]
        public Timestamp ReadTimestamp(ref int sourceOffset)
        {
            if (sourceOffset < 0 || sourceOffset + BitHelper.TimestampSize > length)
                throw new InvalidOperationException(string.Format("Invalid args: sourceOffset = {0}, this = {1}", sourceOffset, this));
            var tmp = offset + sourceOffset;
            var value = BitHelper.ReadTimestamp(bytes, ref tmp);
            sourceOffset += BitHelper.TimestampSize;
            return value;
        }

        public override string ToString()
        {
            return string.Format("Bytes: byte[{0}], Offset: {1}, Length: {2}", bytes.Length, offset, length);
        }

        public bool Equals(ByteRange other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (length != other.length)
                return false;
            return ByteArrayHelpers.memcmp(bytes, offset, other.bytes, other.offset, length) == 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ByteRange)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = length;
            unchecked
            {
                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                    {
                        var i = 0;
                        var currentIntPtr = (int*)(bytesPtr + offset);
                        var intsBound = length - sizeof(int);
                        for (; i <= intsBound; i += sizeof(int), currentIntPtr++)
                            hashCode = (hashCode * 397) ^ (*currentIntPtr);
                        var currentBytePtr = (byte*)(currentIntPtr);
                        for (; i < length; i++, currentBytePtr++)
                            hashCode = (hashCode * 397) ^ (*currentBytePtr);
                        return hashCode;
                    }
                }
            }
        }

        public int CompareTo(ByteRange other)
        {
            if (other == null)
                return 1;
            if (ReferenceEquals(this, other))
                return 0;
            return ByteArrayHelpers.CompareRanges(bytes, offset, length, other.bytes, other.offset, other.length);
        }

        public void WriteTo([NotNull] Stream stream)
        {
            stream.Write(bytes, offset, length);
        }
    }
}