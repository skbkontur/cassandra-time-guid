using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public sealed class TimeGuid : IEquatable<TimeGuid>, IComparable<TimeGuid>
    {
        public TimeGuid([NotNull] Timestamp timestamp, ushort clockSequence, [NotNull] byte[] node)
            : this(TimeGuidFormatter.Format(timestamp, clockSequence, node))
        {
        }

        public TimeGuid(Guid guid)
        {
            if(TimeGuidFormatter.GetVersion(guid) != GuidVersion.TimeBased)
                throw new InvalidProgramStateException(string.Format("Invalid v1 guid: {0}", guid));
            this.guid = guid;
        }

        public static bool TryParse([CanBeNull] string str, out TimeGuid result)
        {
            result = null;
            Guid guid;
            if(!Guid.TryParse(str, out guid))
                return false;
            if(TimeGuidFormatter.GetVersion(guid) != GuidVersion.TimeBased)
                return false;
            result = new TimeGuid(guid);
            return true;
        }

        [NotNull]
        public Timestamp GetTimestamp()
        {
            return TimeGuidFormatter.GetTimestamp(guid);
        }

        public ushort GetClockSequence()
        {
            return TimeGuidFormatter.GetClockSequence(guid);
        }

        [NotNull]
        public byte[] GetNode()
        {
            return TimeGuidFormatter.GetNode(guid);
        }

        public Guid ToGuid()
        {
            return guid;
        }

        public override string ToString()
        {
            return string.Format("Guid: {0}, Timestamp: {1}, ClockSequence: {2}", guid, GetTimestamp(), GetClockSequence());
        }

        public bool Equals([CanBeNull] TimeGuid other)
        {
            if(ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;
            return guid.Equals(other.guid);
        }

        public override bool Equals([CanBeNull] object obj)
        {
            if(ReferenceEquals(null, obj))
                return false;
            if(ReferenceEquals(this, obj))
                return true;
            return obj is TimeGuid && Equals((TimeGuid)obj);
        }

        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }

        /// <remarks>
        /// Cassandra TimeUUIDType first compares the first 0-7 octets as timestamps (time_hi, then time_mid, then time_low)
        /// and then if timestamps are equal compares the last 8-15 octets as signed byte arrays lexicographically
        /// </remarks>
        public int CompareTo([CanBeNull] TimeGuid other)
        {
            if(other == null)
                return 1;
            var result = GetTimestamp().CompareTo(other.GetTimestamp());
            if(result != 0)
                return result;
            var bytes = guid.ToByteArray();
            var otherBytes = other.guid.ToByteArray();
            for(var i = 8; i < bytes.Length; i++)
            {
                if(bytes[i] == otherBytes[i])
                    continue;
                if((bytes[i] ^ 0x80) > (otherBytes[i] ^ 0x80)) // todo (timeguid): refactor and move closer to byte formatting
                    return 1;
                return -1;
            }
            return 0;
        }

        public static bool operator ==(TimeGuid left, TimeGuid right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TimeGuid left, TimeGuid right)
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

        [NotNull]
        public static TimeGuid MinForTimestamp([NotNull] Timestamp timestamp)
        {
            return new TimeGuid(TimeGuidFormatter.Format(timestamp, TimeGuidFormatter.MinClockSequence, TimeGuidFormatter.MinNode));
        }

        [NotNull]
        public static TimeGuid MaxForTimestamp([NotNull] Timestamp timestamp)
        {
            return new TimeGuid(TimeGuidFormatter.Format(timestamp, TimeGuidFormatter.MaxClockSequence, TimeGuidFormatter.MaxNode));
        }

        // ReSharper disable once InconsistentNaming
        // (sic!) guid is not a field for grobuf compatibility
        private Guid guid { get; set; }

        [NotNull]
        public static readonly TimeGuid MinValue = new TimeGuid(TimeGuidFormatter.MinGuid);

        [NotNull]
        public static readonly TimeGuid MaxValue = new TimeGuid(TimeGuidFormatter.MaxGuid);

        private static readonly TimeGuidGenerator guidGen = new TimeGuidGenerator(PreciseTimestampGenerator.Instance);
    }
}