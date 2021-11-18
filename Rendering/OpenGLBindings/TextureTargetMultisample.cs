namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.TexImage2DMultisample, GL.TexImage3DMultisample
    /// </summary>
    public enum TextureTargetMultisample
    {
        /// <summary>
        /// Original was GL_TEXTURE_2D_MULTISAMPLE = 0x9100
        /// </summary>
        Texture2DMultisample = 37120,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_MULTISAMPLE = 0x9101
        /// </summary>
        ProxyTexture2DMultisample,
        /// <summary>
        /// Original was GL_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9102
        /// </summary>
        Texture2DMultisampleArray,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9103
        /// </summary>
        ProxyTexture2DMultisampleArray
    }
}