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

        public long NowTicks()
        {
            lock(stopwatch)
                return DoGetNowTicks();
        }

        private long DoGetNowTicks()
        {
            var nowTicks = GetDateTimeNowTicks();
            var resultTicks = GetResultTicks(nowTicks);
            lastTimestampTicks = resultTicks;
            return resultTicks;
        }

        private long GetDateTimeNowTicks()
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
            var resultTicks = Math.Max(baseTimestampTicks + elapsedTicks, lastTimestampTicks + TicksPerMicrosecond);

            // see http://stackoverflow.com/questions/1008345
            if(elapsedTicks < 0 || Math.Abs(resultTicks - nowTicks) > maxAllowedDivergenceTicks)
                return GetSafeResultTicks(nowTicks);

            return resultTicks;
        }

        private long GetSafeResultTicks(long nowTicks)
        {
            return Math.Max(nowTicks, lastTimestampTicks + TicksPerMicrosecond);
        }

        public const long TicksPerMicrosecond = 10;

        [NotNull]
        public static readonly PreciseTimestampGenerator Instance = new PreciseTimestampGenerator(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100));

        private readonly TimeSpan syncPeriod;
        private readonly long maxAllowedDivergenceTicks;
        private readonly Stopwatch stopwatch;
        private long baseTimestampTicks, lastTimestampTicks;
    }
}