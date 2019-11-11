using JetBrains.Annotations;

namespace SkbKontur.Cassandra.TimeBasedUuid
{
    [PublicAPI]
    public enum GuidVersion
    {
        TimeBased = 1,
        Dce = 2,
        NameBased = 3,
        Random = 4,
    }
}