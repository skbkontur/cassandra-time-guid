using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using SKBKontur.Catalogue.Objects.Bits;

namespace SKBKontur.Catalogue.Objects.TimeBasedUuid
{
    public sealed class TimeGuid : IEquatable<TimeGuid>, IComparable<TimeGuid>, IComparable
    {
        public TimeGuid([NotNull] Timestamp timestamp, ushort clockSequence, [NotNull] byte[] node)
            : this(TimeGuidBitsLayout.Format(timestamp, clockSequence, node))
        {
        }

        public TimeGuid([NotNull] byte[] bytes)
        {
            if(TimeGuidBitsLayout.GetVersion(bytes) != GuidVersion.TimeBased)
                throw new InvalidProgramStateException(string.Format("Invalid v1 guid: [{0}]", string.Join(", ", bytes.Select(x => x.ToString("x2")))));
            this.bytes = bytes;
        }

        public TimeGuid(Guid guid)
        {
            var timeGuidBytes = ReorderGuidBytesInCassandraWay(guid.ToByteArray());
            if(TimeGuidBitsLayout.GetVersion(timeGuidBytes) != GuidVersion.TimeBased)
                throw new InvalidProgramStateException(string.Format("Invalid v1 guid: {0}", guid));
            bytes = timeGuidBytes;
        }

        [NotNull]
        public static TimeGuid Parse([CanBeNull] string str)
        {
            TimeGuid timeGuid;
            if(!TryParse(str, out timeGuid))
                throw new InvalidProgramStateException(string.Format("Cannot parse TimeGuid from: {0}", str));
            return timeGuid;
        }

        public static bool TryParse([CanBeNull] string str, out TimeGuid result)
        {
            result = null;
            Guid guid;
            if(!Guid.TryParse(str, out guid))
                return false;
            var timeGuidBytes = ReorderGuidBytesInCassandraWay(guid.ToByteArray());
            if(TimeGuidBitsLayout.GetVersion(timeGuidBytes) != GuidVersion.TimeBased)
                return false;
            result = new TimeGuid(timeGuidBytes);
            return true;
        }

        [NotNull]
        public Timestamp GetTimestamp()
        {
            return TimeGuidBitsLayout.GetTimestamp(bytes);
        }

        public ushort GetClockSequence()
        {
            return TimeGuidBitsLayout.GetClockSequence(bytes);
        }

        [NotNull]
        public byte[] GetNode()
        {
            return TimeGuidBitsLayout.GetNode(bytes);
        }

        [NotNull]
        public byte[] ToByteArray()
        {
            return bytes;
        }

        public Guid ToGuid()
        {
            return new Guid(ReorderGuidBytesInCassandraWay(bytes));
        }

        public override string ToString()
        {
            return string.Format("Guid: {0}, Timestamp: {1}, ClockSequence: {2}", ToGuid(), GetTimestamp(), GetClockSequence());
        }

        public bool Equals([CanBeNull] TimeGuid other)
        {
            if(ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;
            return ByteArrayComparer.Instance.Equals(bytes, other.bytes);
        }

        public override bool Equals([CanBeNull] object other)
        {
            if(ReferenceEquals(null, other))
                return false;
            if(ReferenceEquals(this, other))
                return true;
            return other is TimeGuid && Equals((TimeGuid)other);
        }

        public override int GetHashCode()
        {
            return ByteArrayComparer.Instance.GetHashCode(bytes);
        }

        /// <remarks>
        ///     Cassandra TimeUUIDType first compares the first 0-7 octets as timestamps (time_hi, then time_mid, then time_low)
        ///     and then if timestamps are equal compares the last 8-15 octets as signed byte arrays lexicographically
        /// </remarks>
        public int CompareTo([CanBeNull] TimeGuid other)
        {
            if(other == null)
                return 1;
            var result = GetTimestamp().CompareTo(other.GetTimestamp());
            if(result != 0)
                return result;
            result = GetClockSequence().CompareTo(other.GetClockSequence());
            if(result != 0)
                return result;
            var node = GetNode();
            var otherNode = other.GetNode();
            if(node.Length != otherNode.Length)
                throw new InvalidProgramStateException(string.Format("Node lengths are different for: {0} and {1}", this, other));
            for(var i = 0; i < node.Length; i++)
            {
                result = node[i].CompareTo(otherNode[i]);
                if(result != 0)
                    return result;
            }
            return 0;
        }

        public int CompareTo([CanBeNull] object other)
        {
            return CompareTo(other as TimeGuid);
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
            return new TimeGuid(TimeGuidBitsLayout.Format(timestamp, TimeGuidBitsLayout.MinClockSequence, TimeGuidBitsLayout.MinNode));
        }

        [NotNull]
        public static TimeGuid MaxForTimestamp([NotNull] Timestamp timestamp)
        {
            return new TimeGuid(TimeGuidBitsLayout.Format(timestamp, TimeGuidBitsLayout.MaxClockSequence, TimeGuidBitsLayout.MaxNode));
        }

        [NotNull]
        private static byte[] ReorderGuidBytesInCassandraWay([NotNull] byte[] b)
        {
            if(b.Length != BitHelper.TimeGuidSize)
                throw new InvalidProgramStateException("b must be 16 bytes long");
            return new[] {b[3], b[2], b[1], b[0], b[5], b[4], b[7], b[6], b[8], b[9], b[10], b[11], b[12], b[13], b[14], b[15]};
        }

        // ReSharper disable once InconsistentNaming
        // (sic!) bytes is not a field for grobuf compatibility
        private byte[] bytes { get; set; }

        [NotNull]
        public static readonly TimeGuid MinValue = new TimeGuid(TimeGuidBitsLayout.MinTimeGuid);

        [NotNull]
        public static readonly TimeGuid MaxValue = new TimeGuid(TimeGuidBitsLayout.MaxTimeGuid);

        private static readonly TimeGuidGenerator guidGen = new TimeGuidGenerator(Timestamp.PreciseTimestampGeneratorInstance);
    }
}