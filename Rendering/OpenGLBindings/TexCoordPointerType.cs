namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Ext.MultiTexCoordPointer, GL.Ext.VertexArrayMultiTexCoordOffset and 1 other function
    /// </summary>
    public enum TexCoordPointerType
    {
        /// <summary>
        /// Original was GL_SHORT = 0x1402
        /// </summary>
        Short = 5122,
        /// <summary>
        /// Original was GL_INT = 0x1404
        /// </summary>
        Int = 5124,
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
        /// Original was GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368
        /// </summary>
        UnsignedInt2101010Rev = 33640,
        /// <summary>
        /// Original was GL_INT_2_10_10_10_REV = 0x8D9F
        /// </summary>
        Int2101010Rev = 36255
    }
}