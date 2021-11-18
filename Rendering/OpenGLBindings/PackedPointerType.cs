namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.ColorP3, GL.ColorP4 and 17 other functions
    /// </summary>
    public enum PackedPointerType
    {
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