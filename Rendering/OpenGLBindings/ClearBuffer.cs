namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.ClearBuffer, GL.ClearNamedFramebuffer
    /// </summary>
    public enum ClearBuffer
    {
        /// <summary>
        /// Original was GL_COLOR = 0x1800
        /// </summary>
        Color = 6144,
        /// <summary>
        /// Original was GL_DEPTH = 0x1801
        /// </summary>
        Depth,
        /// <summary>
        /// Original was GL_STENCIL = 0x1802
        /// </summary>
        Stencil
    }
}