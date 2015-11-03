using System;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.BitConversion
{
    public static class ByteArrayHelpers
    {
        public static string GetGuidStringSafely([CanBeNull] this byte[] guid)
        {
            if (guid == null)
                return "<null>";
            try
            {
                return new Guid(guid).ToString();
            }
            catch (Exception)
            {
                return Convert.ToBase64String(guid);
            }
        }

        public static byte[] Concat(this byte[] self, byte[] other)
        {
            if (other.Length == 0)
                return self;
            if (self.Length == 0)
                return other;
            var result = new byte[self.Length + other.Length];
            Array.Copy(self, result, self.Length);
            Array.Copy(other, 0, result, self.Length, other.Length);
            return result;
        }

        public static byte[] Slice(this byte[] self, int offset, int? length = null)
        {
            if (offset < 0)
                throw new ArgumentException();
            if (length.HasValue && length.Value < 0)
                throw new ArgumentException();
            var resultLength = length.HasValue ? length.Value : self.Length - offset;
            if (offset + resultLength > self.Length)
                throw new ArgumentException(string.Format("array length [{0}], offset [{1}], length [{2}]", self.Length, offset, resultLength));
            var result = new byte[resultLength];
            Array.Copy(self, offset, result, 0, resultLength);
            return result;
        }

        public static bool PrefixIsEqualTo([NotNull] this byte[] bytes, [NotNull] byte[] prefix)
        {
            return bytes.PrefixIsEqualTo(prefix, prefix.Length);
        }

        public static bool PrefixIsEqualTo([NotNull] this byte[] bytes, [NotNull] byte[] prefix, int prefixLength)
        {
            if (prefix.Length < prefixLength)
                throw new InvalidOperationException(String.Format("Prefix.Length ({0}) is less than prefixLength ({1})", prefix.Length, prefixLength));
            if (bytes.Length < prefixLength)
                return false;
            var prefixRange = new ByteRange(prefix, 0, prefixLength);
            var bytesRange = new ByteRange(bytes, 0, prefixLength);
            return prefixRange.Equals(bytesRange);
        }

        /// <remarks>
        ///     Code from
        ///     https://github.com/ravendb/ravendb/blob/a13de74595846cac6ee5e254a13ef868d2b31779/Raven.Voron/Voron/Util/MemoryUtils.cs#L11
        /// </remarks>
        public static unsafe int memcmp(byte[] b1, int b1Offset, byte[] b2, int b2Offset, long n)
        {
            if (n == 0)
                return 0;
            fixed (byte* pb1 = b1)
            fixed (byte* pb2 = b2)
            {
                var lhs = pb1 + b1Offset;
                var rhs = pb2 + b2Offset;
                const int sizeOfUInt = BitHelper.UintSize;

                if (n > sizeOfUInt)
                {
                    var lUintAlignment = (long)lhs % sizeOfUInt;
                    var rUintAlignment = (long)rhs % sizeOfUInt;

                    if (lUintAlignment != 0 && lUintAlignment == rUintAlignment)
                    {
                        var toAlign = sizeOfUInt - lUintAlignment;
                        while (toAlign > 0)
                        {
                            var r = *lhs++ - *rhs++;
                            if (r != 0)
                                return r;
                            n--;

                            toAlign--;
                        }
                    }

                    var lp = (uint*)lhs;
                    var rp = (uint*)rhs;

                    while (n > sizeOfUInt)
                    {
                        if (*lp != *rp)
                            break;

                        lp++;
                        rp++;

                        n -= sizeOfUInt;
                    }

                    lhs = (byte*)lp;
                    rhs = (byte*)rp;
                }

                while (n > 0)
                {
                    var r = *lhs++ - *rhs++;
                    if (r != 0)
                        return r;
                    n--;
                }

                return 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareTo(this byte[] x, byte[] y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            if (y == null)
                return 1;
            if (ReferenceEquals(x, y))
                return 0;
            return CompareRanges(x, 0, x.Length, y, 0, y.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LessThan(this byte[] x, byte[] y)
        {
            return x.CompareTo(y) < 0;
        }

        public static int CompareRanges(byte[] b1, int b1Offset, int b1Length, byte[] b2, int b2Offset, int b2Length)
        {
            if (b1Length == b2Length)
                return CompareTo(b1, b1Offset, b2, b2Offset, b1Length);
            if (b1Length < b2Length)
            {
                var res = CompareTo(b1, b1Offset, b2, b2Offset, b1Length);
                return res != 0 ? res : -1;
            }
            else
            {
                var res = CompareTo(b1, b1Offset, b2, b2Offset, b2Length);
                return res != 0 ? res : 1;
            }
        }

        private static int CompareTo(byte[] b1, int b1Offset, byte[] b2, int b2Offset, int count)
        {
            return count == 0 ? 0 : memcmp(b1, b1Offset, b2, b2Offset, count);
        }
    }
}