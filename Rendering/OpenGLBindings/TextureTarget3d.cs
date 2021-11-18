namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.TexStorage3D
    /// </summary>
    public enum TextureTarget3d
    {
        /// <summary>
        /// Original was GL_TEXTURE_3D = 0x806F
        /// </summary>
        Texture3D = 32879,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_3D = 0x8070
        /// </summary>
        ProxyTexture3D = 32880,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP = 0x8513
        /// </summary>
        TextureCubeMap = 34067,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_CUBE_MAP = 0x851B
        /// </summary>
        ProxyTextureCubeMap = 34075,
        /// <summary>
        /// Original was GL_TEXTURE_2D_ARRAY = 0x8C1A
        /// </summary>
        Texture2DArray = 35866,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_ARRAY = 0x8C1B
        /// </summary>
        ProxyTexture2DArray = 35867
    }
}