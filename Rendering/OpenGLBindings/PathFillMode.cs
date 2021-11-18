namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.NV.StencilFillPathInstanced, GL.NV.StencilFillPath
    /// </summary>
    public enum PathFillMode
    {
        /// <summary>
        /// Original was GL_INVERT = 0x150A
        /// </summary>
        Invert = 5386,
        /// <summary>
        /// Original was GL_PATH_FILL_MODE_NV = 0x9080
        /// </summary>
        PathFillModeNv = 36992,
        /// <summary>
        /// Original was GL_COUNT_UP_NV = 0x9088
        /// </summary>
        CountUpNv = 37000,
        /// <summary>
        /// Original was GL_COUNT_DOWN_NV = 0x9089
        /// </summary>
        CountDownNv = 37001
    }
}