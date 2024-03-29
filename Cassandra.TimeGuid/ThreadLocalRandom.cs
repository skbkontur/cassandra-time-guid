using System;
using System.Threading;

using JetBrains.Annotations;

namespace SkbKontur.Cassandra.TimeBasedUuid
{
    public static class ThreadLocalRandom
    {
        [NotNull]
        public static Random Instance => threadLocalRandom.Value;

        private static readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() =>
            {
                lock (globalRandom)
                    return new Random(globalRandom.Next());
            });

        private static readonly Random globalRandom = new Random(Environment.TickCount);
    }
}