using System;

using JetBrains.Annotations;

using SKBKontur.Catalogue.Objects.TimeBasedUuid;

namespace SKBKontur.Catalogue.Objects
{
    public sealed class TimeGuid : IEquatable<TimeGuid>, IComparable<TimeGuid>, IComparable
    {
        private static readonly TimeGuidGenerator guidGen = new TimeGuidGenerator(PreciseTimestampGenerator.Instance);

        [NotNull]
        public static readonly TimeGuid MinValue = new TimeGuid(TimeGuidGenerator.MinGuid);

        [NotNull]
        public static readonly TimeGuid MaxValue = new TimeGuid(TimeGuidGenerator.MaxGuid);

        [NotNull]
        public static TimeGuid MinForTimestamp([NotNull] Timestamp timestamp)
        {
            return new TimeGuid(TimeGuidGenerator.MinGuidForTimestamp(timestamp));
        }

        [NotNull]
        public static TimeGuid MaxForTimestamp([NotNull] Timestamp timestamp)
        {
            return new TimeGuid(TimeGuidGenerator.MaxGuidForTimestamp(timestamp));
        }

        [NotNull]
        public static TimeGuid NowGuid()
        {
            return new TimeGuid(guidGen.NewGuid());
        }

        [NotNull]
        public static TimeGuid NewGuid([NotNull] Timestamp timestamp)
        {
            return new TimeGuid(guidGen.NewGuid(timestamp));
        }

        [NotNull]
        public static TimeGuid NewGuid([NotNull] Timestamp timestamp, byte clockSequence)
        {
            return new TimeGuid(guidGen.NewGuid(timestamp, clockSequence));
        }

        private readonly Guid guid;

        [UsedImplicitly]
        private TimeGuid()
        {
        }

        public TimeGuid([NotNull] Timestamp timestamp, byte clockSequence, [NotNull] byte[] node)
            : this(TimeGuidFormatter.Format(timestamp, clockSequence, node))
        {
        }

        public TimeGuid(Guid guid)
        {
            if (TimeGuidFormatter.GetVersion(guid) != GuidVersion.TimeBased)
                throw new InvalidOperationException(string.Format("Invalid v1 guid: {0}", guid));
            this.guid = guid;
        }

        public Guid ToGuid()
        {
            return guid;
        }

        [NotNull]
        public Timestamp GetTimestamp()
        {
            return TimeGuidFormatter.GetTimestamp(guid);
        }

        public byte GetClockSequence()
        {
            return TimeGuidFormatter.GetClockSequence(guid);
        }

        [NotNull]
        public byte[] GetNode()
        {
            return TimeGuidFormatter.GetNode(guid);
        }

        public override string ToString()
        {
            return string.Format("Guid: {0}, Timestamp: {1}", guid, GetTimestamp());
        }

        public bool Equals([CanBeNull] TimeGuid other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return guid == other.guid;
        }

        public override bool Equals([CanBeNull] object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((TimeGuid)obj);
        }

        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }

        public static bool operator ==([CanBeNull] TimeGuid left, [CanBeNull] TimeGuid right)
        {
            return Equals(left, right);
        }

        public static bool operator !=([CanBeNull] TimeGuid left, [CanBeNull] TimeGuid right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(TimeGuid other)
        {
            if (other == null)
                return 1;
            var result = GetTimestamp().CompareTo(other.GetTimestamp());
            if (result == 0)
                return ToGuid().CompareTo(other.ToGuid());
            return result;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as TimeGuid);
        }
    }
}