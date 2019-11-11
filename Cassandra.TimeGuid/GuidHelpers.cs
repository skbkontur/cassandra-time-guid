using System;

namespace SkbKontur.Cassandra.TimeGuid
{
    public static class GuidHelpers
    {
        public static readonly Guid MinGuid = Guid.Empty;
        public static readonly Guid MaxGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }
}