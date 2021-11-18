namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.TexStorage2D
    /// </summary>
    public enum TextureTarget2d
    {
        /// <summary>
        /// Original was GL_TEXTURE_2D = 0x0DE1
        /// </summary>
        Texture2D = 3553,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D = 0x8064
        /// </summary>
        ProxyTexture2D = 32868,
        /// <summary>
        /// Original was GL_TEXTURE_RECTANGLE = 0x84F5
        /// </summary>
        TextureRectangle = 34037,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_RECTANGLE = 0x84F7
        /// </summary>
        ProxyTextureRectangle = 34039,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP = 0x8513
        /// </summary>
        TextureCubeMap = 34067,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_CUBE_MAP = 0x851B
        /// </summary>
        ProxyTextureCubeMap = 34075,
        /// <summary>
        /// Original was GL_TEXTURE_1D_ARRAY = 0x8C18
        /// </summary>
        Texture1DArray = 35864,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_1D_ARRAY = 0x8C19
        /// </summary>
        ProxyTexture1DArray = 35865
    }
}