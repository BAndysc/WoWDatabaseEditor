namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.BlitFramebuffer, GL.BlitNamedFramebuffer and 1 other function
    /// </summary>
    [Flags]
    public enum ClearBufferMask
    {
        /// <summary>
        /// Original was GL_NONE = 0
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Original was GL_DEPTH_BUFFER_BIT = 0x00000100
        /// </summary>
        DepthBufferBit = 0x100,
        /// <summary>
        /// Original was GL_ACCUM_BUFFER_BIT = 0x00000200
        /// </summary>
        AccumBufferBit = 0x200,
        /// <summary>
        /// Original was GL_STENCIL_BUFFER_BIT = 0x00000400
        /// </summary>
        StencilBufferBit = 0x400,
        /// <summary>
        /// Original was GL_COLOR_BUFFER_BIT = 0x00004000
        /// </summary>
        ColorBufferBit = 0x4000,
        /// <summary>
        /// Original was GL_COVERAGE_BUFFER_BIT_NV = 0x00008000
        /// </summary>
        CoverageBufferBitNv = 0x8000
    }
}