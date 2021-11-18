namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.VertexArrayAttribFormat, GL.VertexArrayAttribIFormat and 3 other functions
    /// </summary>
    public enum VertexAttribType
    {
        /// <summary>
        /// Original was GL_BYTE = 0x1400
        /// </summary>
        Byte = 5120,
        /// <summary>
        /// Original was GL_UNSIGNED_BYTE = 0x1401
        /// </summary>
        UnsignedByte = 5121,
        /// <summary>
        /// Original was GL_SHORT = 0x1402
        /// </summary>
        Short = 5122,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT = 0x1403
        /// </summary>
        UnsignedShort = 5123,
        /// <summary>
        /// Original was GL_INT = 0x1404
        /// </summary>
        Int = 5124,
        /// <summary>
        /// Original was GL_UNSIGNED_INT = 0x1405
        /// </summary>
        UnsignedInt = 5125,
        /// <summary>
        /// Original was GL_FLOAT = 0x1406
        /// </summary>
        Float = 5126,
        /// <summary>
        /// Original was GL_DOUBLE = 0x140A
        /// </summary>
        Double = 5130,
        /// <summary>
        /// Original was GL_HALF_FLOAT = 0x140B
        /// </summary>
        HalfFloat = 5131,
        /// <summary>
        /// Original was GL_FIXED = 0x140C
        /// </summary>
        Fixed = 5132,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368
        /// </summary>
        UnsignedInt2101010Rev = 33640,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B
        /// </summary>
        UnsignedInt10F11F11FRev = 35899,
        /// <summary>
        /// Original was GL_INT_2_10_10_10_REV = 0x8D9F
        /// </summary>
        Int2101010Rev = 36255
    }
}