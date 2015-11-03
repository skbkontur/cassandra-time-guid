using System;

namespace SKBKontur.Catalogue.Objects
{
    public static class GuidHelpers
    {
        public static readonly Guid MinGuid = Guid.Empty;
        public static readonly Guid MaxGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    }
}