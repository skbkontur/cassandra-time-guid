using System;
using System.Collections.Generic;

namespace SKBKontur.Catalogue.Objects.BitConversion
{
    public class ByteArrayComparer : IEqualityComparer<byte[]>, IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new ByteArrayComparer();

        public bool Equals(byte[] x, byte[] y)
        {
            if (x == null ^ y == null)
                return false;
            if (ReferenceEquals(x, y))
                return true;

            if (x.Length != y.Length)
                return false;
            return ByteArrayHelpers.memcmp(x, 0, y, 0, x.Length) == 0;
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null)
                throw new ArgumentNullException();
            var b1 = obj.Length > 0 ? obj[0] : 0;
            var b2 = obj.Length > 1 ? obj[1] : 0;
            var b3 = obj.Length > 2 ? obj[2] : 0;
            var b4 = obj.Length > 3 ? obj[3] : 0;
            return b1 + (b2 << 8) + (b3 << 16) + (b4 << 24);
        }

        public int Compare(byte[] x, byte[] y)
        {
            return x.CompareTo(y);
        }
    }
}