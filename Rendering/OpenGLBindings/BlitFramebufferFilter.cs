namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.BlitFramebuffer, GL.BlitNamedFramebuffer
    /// </summary>
    public enum BlitFramebufferFilter
    {
        /// <summary>
        /// Original was GL_NEAREST = 0x2600
        /// </summary>
        Nearest = 9728,
        /// <summary>
        /// Original was GL_LINEAR = 0x2601
        /// </summary>
        Linear
    }
}