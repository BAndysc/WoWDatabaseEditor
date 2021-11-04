namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.FramebufferSampleLocations, GL.Arb.FramebufferTexture and 19 other functions
    /// </summary>
    public enum FramebufferTarget
    {
        /// <summary>
        /// Original was GL_READ_FRAMEBUFFER = 0x8CA8
        /// </summary>
        ReadFramebuffer = 36008,
        /// <summary>
        /// Original was GL_DRAW_FRAMEBUFFER = 0x8CA9
        /// </summary>
        DrawFramebuffer = 36009,
        /// <summary>
        /// Original was GL_FRAMEBUFFER = 0x8D40
        /// </summary>
        Framebuffer = 36160,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_EXT = 0x8D40
        /// </summary>
        FramebufferExt = 36160
    }
}