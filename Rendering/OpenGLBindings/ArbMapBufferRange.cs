namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    public enum ArbMapBufferRange
    {
        /// <summary>
        /// Original was GL_MAP_READ_BIT = 0x0001
        /// </summary>
        MapReadBit = 1,
        /// <summary>
        /// Original was GL_MAP_WRITE_BIT = 0x0002
        /// </summary>
        MapWriteBit = 2,
        /// <summary>
        /// Original was GL_MAP_INVALIDATE_RANGE_BIT = 0x0004
        /// </summary>
        MapInvalidateRangeBit = 4,
        /// <summary>
        /// Original was GL_MAP_INVALIDATE_BUFFER_BIT = 0x0008
        /// </summary>
        MapInvalidateBufferBit = 8,
        /// <summary>
        /// Original was GL_MAP_FLUSH_EXPLICIT_BIT = 0x0010
        /// </summary>
        MapFlushExplicitBit = 0x10,
        /// <summary>
        /// Original was GL_MAP_UNSYNCHRONIZED_BIT = 0x0020
        /// </summary>
        MapUnsynchronizedBit = 0x20
    }
}