namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.DrawElementsInstanced, GL.Arb.MultiDrawElementsIndirectCount and 15 other functions
    /// </summary>
    public enum DrawElementsType
    {
        /// <summary>
        /// Original was GL_UNSIGNED_BYTE = 0x1401
        /// </summary>
        UnsignedByte = 5121,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT = 0x1403
        /// </summary>
        UnsignedShort = 5123,
        /// <summary>
        /// Original was GL_UNSIGNED_INT = 0x1405
        /// </summary>
        UnsignedInt = 5125
    }
}