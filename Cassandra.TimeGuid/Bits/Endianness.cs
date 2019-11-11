/*
	"Miscellaneous Utility Library" Software Licence
	Version 1.0
	Copyright (c) 2004-2008 Jon Skeet and Marc Gravell. All rights reserved.
*/

using JetBrains.Annotations;

namespace SkbKontur.Cassandra.TimeBasedUuid.Bits
{
    /// <summary>
    ///     Endianness of a converter
    /// </summary>
    [PublicAPI]
    public enum Endianness
    {
        /// <summary>
        ///     Little endian - least significant byte first
        /// </summary>
        LittleEndian,

        /// <summary>
        ///     Big endian - most significant byte first
        /// </summary>
        BigEndian
    }
}