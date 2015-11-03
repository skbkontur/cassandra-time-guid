namespace SKBKontur.Catalogue.Objects.BitConversion
{
    public static class ByteArrayHelpers
    {
        public static bool Equals(byte[] x, byte[] y)
        {
            if(x == null ^ y == null)
                return false;
            if(ReferenceEquals(x, y))
                return true;
            if(x.Length != y.Length)
                return false;
            return memcmp(x, 0, y, 0, x.Length) == 0;
        }

        public static int Compare(byte[] x, byte[] y)
        {
            if(x == null)
                return y == null ? 0 : -1;
            if(y == null)
                return 1;
            if(ReferenceEquals(x, y))
                return 0;
            return CompareRanges(x, 0, x.Length, y, 0, y.Length);
        }

        public static int CompareRanges(byte[] b1, int b1Offset, int b1Length, byte[] b2, int b2Offset, int b2Length)
        {
            if (b1Length == b2Length)
                return memcmp(b1, b1Offset, b2, b2Offset, b1Length);
            if (b1Length < b2Length)
            {
                var res = memcmp(b1, b1Offset, b2, b2Offset, b1Length);
                return res != 0 ? res : -1;
            }
            else
            {
                var res = memcmp(b1, b1Offset, b2, b2Offset, b2Length);
                return res != 0 ? res : 1;
            }
        }

        public static bool LessThan(this byte[] x, byte[] y)
        {
            return Compare(x, y) < 0;
        }

        public static bool GreaterThan(this byte[] x, byte[] y)
        {
            return Compare(x, y) > 0;
        }

        /// <remarks>
        ///     Code from
        ///     https://github.com/ravendb/ravendb/blob/a13de74595846cac6ee5e254a13ef868d2b31779/Raven.Voron/Voron/Util/MemoryUtils.cs#L11
        /// </remarks>
        private static unsafe int memcmp(byte[] b1, int b1Offset, byte[] b2, int b2Offset, long n)
        {
            if(n == 0)
                return 0;
            fixed(byte* pb1 = b1)
            {
                fixed(byte* pb2 = b2)
                {
                    var lhs = pb1 + b1Offset;
                    var rhs = pb2 + b2Offset;
                    const int sizeOfUInt = BitHelper.UintSize;

                    if(n > sizeOfUInt)
                    {
                        var lUintAlignment = (long)lhs % sizeOfUInt;
                        var rUintAlignment = (long)rhs % sizeOfUInt;

                        if(lUintAlignment != 0 && lUintAlignment == rUintAlignment)
                        {
                            var toAlign = sizeOfUInt - lUintAlignment;
                            while(toAlign > 0)
                            {
                                var r = *lhs++ - *rhs++;
                                if(r != 0)
                                    return r;
                                n--;

                                toAlign--;
                            }
                        }

                        var lp = (uint*)lhs;
                        var rp = (uint*)rhs;

                        while(n > sizeOfUInt)
                        {
                            if(*lp != *rp)
                                break;

                            lp++;
                            rp++;

                            n -= sizeOfUInt;
                        }

                        lhs = (byte*)lp;
                        rhs = (byte*)rp;
                    }

                    while(n > 0)
                    {
                        var r = *lhs++ - *rhs++;
                        if(r != 0)
                            return r;
                        n--;
                    }

                    return 0;
                }
            }
        }
    }
}