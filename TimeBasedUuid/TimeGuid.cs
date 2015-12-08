using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using SKBKontur.Catalogue.Objects.TimeBasedUuid;

namespace SKBKontur.Catalogue.Objects
{
    public sealed class TimeGuid : IEquatable<TimeGuid>, IComparable<TimeGuid>, IComparable
    {
        public TimeGuid([NotNull] Timestamp timestamp, ushort clockSequence, [NotNull] byte[] node)
            : this(TimeGuidFormatter.Format(timestamp, clockSequence, node))
        {
        }

        public TimeGuid(Guid guid)
        {
            if(TimeGuidFormatter.GetVersion(guid) != GuidVersion.TimeBased)
                throw new InvalidOperationException(string.Format("Invalid v1 guid: {0}", guid));
            this.Guid = guid;
        }

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
        public static TimeGuid NewGuid([NotNull] Timestamp timestamp, ushort clockSequence)
        {
            return new TimeGuid(guidGen.NewGuid(timestamp, clockSequence));
        }

        public Guid ToGuid()
        {
            return Guid;
        }

        [NotNull]
        public Timestamp GetTimestamp()
        {
            return TimeGuidFormatter.GetTimestamp(Guid);
        }

        public ushort GetClockSequence()
        {
            return TimeGuidFormatter.GetClockSequence(Guid);
        }

        [NotNull]
        public byte[] GetNode()
        {
            return TimeGuidFormatter.GetNode(Guid);
        }

        public static bool TryParse(string input, out TimeGuid result)
        {
            result = null;
            Guid guid;
            if(!Guid.TryParse(input, out guid))
                return false;
            if(TimeGuidFormatter.GetVersion(guid) != GuidVersion.TimeBased)
                return false;

            result = new TimeGuid(guid);
            return true;
        }

        public override string ToString()
        {
            return string.Format("Guid: {0}, Timestamp: {1}", Guid, GetTimestamp());
        }

        public bool Equals([CanBeNull] TimeGuid other)
        {
            if(ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;
            return Guid == other.Guid;
        }

        public override bool Equals([CanBeNull] object obj)
        {
            if(ReferenceEquals(null, obj))
                return false;
            if(ReferenceEquals(this, obj))
                return true;
            if(obj.GetType() != GetType())
                return false;
            return Equals((TimeGuid)obj);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public static bool operator ==([CanBeNull] TimeGuid left, [CanBeNull] TimeGuid right)
        {
            return Equals(left, right);
        }

        public static bool operator !=([CanBeNull] TimeGuid left, [CanBeNull] TimeGuid right)
        {
            return !Equals(left, right);
        }

        public static bool operator >([CanBeNull] TimeGuid left, [CanBeNull] TimeGuid right)
        {
            return Comparer<TimeGuid>.Default.Compare(left, right) > 0;
        }

        public static bool operator <(TimeGuid left, TimeGuid right)
        {
            return Comparer<TimeGuid>.Default.Compare(left, right) < 0;
        }

        public static bool operator >=([CanBeNull] TimeGuid left, [CanBeNull] TimeGuid right)
        {
            return Comparer<TimeGuid>.Default.Compare(left, right) >= 0;
        }

        public static bool operator <=(TimeGuid left, TimeGuid right)
        {
            return Comparer<TimeGuid>.Default.Compare(left, right) <= 0;
        }

        public int CompareTo(TimeGuid other)
        {
            if(other == null)
                return 1;
            var result = GetTimestamp().CompareTo(other.GetTimestamp());
            if(result == 0)
            {
                var bytes = ToGuid().ToByteArray();
                var otherBytes = other.ToGuid().ToByteArray();
                for(var i = 8; i < bytes.Length; i++)
                {
                    if(bytes[i] == otherBytes[i])
                        continue;

                    if((bytes[i] ^ 0x80) > (otherBytes[i] ^ 0x80))
                        return 1;
                    return -1;
                }
            }
            return result;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as TimeGuid);
        }

        [NotNull]
        public static readonly TimeGuid MinValue = new TimeGuid(TimeGuidGenerator.MinGuid);

        [NotNull]
        public static readonly TimeGuid MaxValue = new TimeGuid(TimeGuidGenerator.MaxGuid);

        private Guid Guid { get; set; }
        private static readonly TimeGuidGenerator guidGen = new TimeGuidGenerator(PreciseTimestampGenerator.Instance);
    }
}