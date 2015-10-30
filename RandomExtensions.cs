using System;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects
{
    public static class RandomExtensions
    {
        public static byte NextByte([NotNull] this Random random)
        {
            return (byte)random.Next();
        }

        [NotNull]
        public static byte[] NextBytes([NotNull] this Random random, int length)
        {
            var buf = new byte[length];
            random.NextBytes(buf);
            return buf;
        }
    }
}