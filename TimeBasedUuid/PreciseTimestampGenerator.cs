using System;
using System.Diagnostics;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public class PreciseTimestampGenerator
    {
        public PreciseTimestampGenerator(TimeSpan syncPeriod, TimeSpan maxAllowedDivergence)
        {
            this.syncPeriod = syncPeriod;
            maxAllowedDivergenceTicks = maxAllowedDivergence.Ticks;
            baseTimestampTicks = DateTime.UtcNow.Ticks;
            lastTimestampTicks = baseTimestampTicks;
            stopwatch = Stopwatch.StartNew();
        }

        [NotNull]
        public Timestamp Now()
        {
            lock(stopwatch)
                return DoGetNow();
        }

        [NotNull]
        private Timestamp DoGetNow()
        {
            var nowTicks = GetNowTicks();
            var resultTicks = GetResultTicks(nowTicks);
            lastTimestampTicks = resultTicks;
            return new Timestamp(resultTicks);
        }

        private long GetNowTicks()
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            if(stopwatch.Elapsed > syncPeriod)
            {
                baseTimestampTicks = nowTicks;
                stopwatch.Restart();
            }
            return nowTicks;
        }

        private long GetResultTicks(long nowTicks)
        {
            var elapsedTicks = stopwatch.Elapsed.Ticks;
            var resultTicks = Math.Max(baseTimestampTicks + elapsedTicks, lastTimestampTicks + 1);

            // see http://stackoverflow.com/questions/1008345
            if(elapsedTicks < 0 || Math.Abs(resultTicks - nowTicks) > maxAllowedDivergenceTicks)
                return GetSafeResultTicks(nowTicks);

            return resultTicks;
        }

        private long GetSafeResultTicks(long nowTicks)
        {
            return Math.Max(nowTicks, lastTimestampTicks + 1);
        }

        [NotNull]
        public static readonly PreciseTimestampGenerator Instance = new PreciseTimestampGenerator(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100));

        private readonly TimeSpan syncPeriod;
        private readonly long maxAllowedDivergenceTicks;
        private readonly Stopwatch stopwatch;
        private long baseTimestampTicks, lastTimestampTicks;
    }
}