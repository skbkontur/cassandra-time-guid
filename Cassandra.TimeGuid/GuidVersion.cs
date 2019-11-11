using JetBrains.Annotations;

namespace SkbKontur.Cassandra.TimeGuid
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