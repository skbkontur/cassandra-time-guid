using System;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects
{
    public static class TimestampExtensions
    {
        [NotNull]
        public static Timestamp Floor([NotNull] this Timestamp timestamp, TimeSpan precision)
        {
            if (precision.Ticks <= 0)
                throw new InvalidOperationException($"Could not run Floor with {precision} precision");
            return new Timestamp((timestamp.Ticks / precision.Ticks) * precision.Ticks);
        }
    }
}