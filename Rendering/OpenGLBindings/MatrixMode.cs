namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Ext.MatrixFrustum, GL.Ext.MatrixLoad and 10 other functions
    /// </summary>
    public enum MatrixMode
    {
        /// <summary>
        /// Original was GL_MODELVIEW0_EXT = 0x1700
        /// </summary>
        Modelview0Ext = 5888,
        /// <summary>
        /// Original was GL_TEXTURE = 0x1702
        /// </summary>
        Texture = 5890,
        /// <summary>
        /// Original was GL_COLOR = 0x1800
        /// </summary>
        Color = 6144
    }
}