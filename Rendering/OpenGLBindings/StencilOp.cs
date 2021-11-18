namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.StencilOp, GL.StencilOpSeparate
    /// </summary>
    public enum StencilOp
    {
        /// <summary>
        /// Original was GL_ZERO = 0
        /// </summary>
        Zero = 0,
        /// <summary>
        /// Original was GL_INVERT = 0x150A
        /// </summary>
        Invert = 5386,
        /// <summary>
        /// Original was GL_KEEP = 0x1E00
        /// </summary>
        Keep = 7680,
        /// <summary>
        /// Original was GL_REPLACE = 0x1E01
        /// </summary>
        Replace = 7681,
        /// <summary>
        /// Original was GL_INCR = 0x1E02
        /// </summary>
        Incr = 7682,
        /// <summary>
        /// Original was GL_DECR = 0x1E03
        /// </summary>
        Decr = 7683,
        /// <summary>
        /// Original was GL_INCR_WRAP = 0x8507
        /// </summary>
        IncrWrap = 34055,
        /// <summary>
        /// Original was GL_DECR_WRAP = 0x8508
        /// </summary>
        DecrWrap = 34056
    }
}