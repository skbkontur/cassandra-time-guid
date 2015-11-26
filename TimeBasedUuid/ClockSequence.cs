using System;
using System.Collections.Generic;

using SKBKontur.Catalogue.Objects.BitConversion;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public struct ClockSequence : IEquatable<ClockSequence>, IComparable<ClockSequence>
    {
        private ClockSequence(ushort index)
        {
            if(index > maxIndex)
                throw new ArgumentOutOfRangeException(string.Format("index must not be greater than {0}", maxIndex));
            this.index = index;
            var bytes = EndianBitConverter.Big.GetBytes(index);
            hByte = bytes[0];
            lByte = (byte)(bytes[1] ^ clockSequenceLowByteMask);
        }

        public ClockSequence(byte[] bytes)
        {
            if(bytes.Length != 2)
                throw new ArgumentException("bytes must be 2 bytes long");

            hByte = (byte)(bytes[0] & clockSequenceHighByteMask);
            lByte = bytes[1];

            index = EndianBitConverter.Big.ToUInt16(new[] {hByte, (byte)(lByte ^ clockSequenceLowByteMask)}, 0);
        }

        public ClockSequence Next()
        {
            if(index == maxIndex)
                throw new InvalidOperationException("next clockSequence not existing");

            return new ClockSequence((ushort)(index + 1));
        }

        public byte[] GetBytes()
        {
            return new[] {hByte, lByte};
        }

        public bool Equals(ClockSequence other)
        {
            return index == other.index && hByte == other.hByte && lByte == other.lByte;
        }

        public override bool Equals(object obj)
        {
            if(obj == null)
                return false;
            return obj is ClockSequence && Equals((ClockSequence)obj);
        }

        public override int GetHashCode()
        {
            return index.GetHashCode();
        }

        public static bool operator ==(ClockSequence left, ClockSequence right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClockSequence left, ClockSequence right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(ClockSequence other)
        {
            return Comparer<ushort>.Default.Compare(index, other.index);
        }

        public override string ToString()
        {
            return index.ToString("D5");
        }

        private const int clockSequenceLowByteMask = 0x80;
        private const byte clockSequenceHighByteMask = 0x3f;
        private const ushort maxIndex = 16383;
        public static readonly ClockSequence MinValue = new ClockSequence(new byte[] {0x00, 0x80});
        public static readonly ClockSequence MaxValue = new ClockSequence(new byte[] {0x3f, 0x7f});
        private readonly ushort index;
        private readonly byte hByte;
        private readonly byte lByte;
    }
}