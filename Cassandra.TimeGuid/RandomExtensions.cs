using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace SkbKontur.Cassandra.TimeGuid
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

        public static uint NextUint([NotNull] this Random random)
        {
            return (uint)random.Next();
        }

        public static ushort NextUshort([NotNull] this Random random)
        {
            return random.NextUshort(ushort.MinValue, ushort.MaxValue);
        }

        public static ushort NextUshort([NotNull] this Random random, ushort minValue, ushort maxValue)
        {
            return (ushort)random.Next(minValue, maxValue);
        }

        public static long NextLong([NotNull] this Random random)
        {
            var highBits = ((long)random.Next()) << 32;
            var lowBits = (long)random.Next();
            return highBits + lowBits;
        }

        public static ulong NextUlong([NotNull] this Random random)
        {
            var highBits = ((ulong)random.Next()) << 32;
            var lowBits = (ulong)random.Next();
            return highBits + lowBits;
        }

        public static DateTime NextDateTime([NotNull] this Random random)
        {
            var ticks = (long)(random.NextDouble() * (DateTime.MaxValue.Ticks - DateTime.MinValue.Ticks) + DateTime.MinValue.Ticks);
            return new DateTime(ticks);
        }

        public static TimeSpan NextTimeSpan([NotNull] this Random random, TimeSpan maxValue)
        {
            return TimeSpan.FromTicks((long)(random.NextDouble() * maxValue.Ticks));
        }

        [NotNull]
        public static Timestamp NextTimestamp([NotNull] this Random random)
        {
            var ticks = (long)(random.NextDouble() * (Timestamp.MaxValue.Ticks - Timestamp.MinValue.Ticks) + Timestamp.MinValue.Ticks);
            return new Timestamp(ticks);
        }

        public static Guid NextGuid([NotNull] this Random random)
        {
            return new Guid(random.NextBytes(16));
        }

        public static void Shuffle<T>([NotNull] this Random random, [NotNull] List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var toSwap = random.Next(i, list.Count);
                var temp = list[i];
                list[i] = list[toSwap];
                list[toSwap] = temp;
            }
        }
    }
}