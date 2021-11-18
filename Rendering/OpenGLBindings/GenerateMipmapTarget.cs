namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GenerateMipmap
    /// </summary>
    public enum GenerateMipmapTarget
    {
        /// <summary>
        /// Original was GL_TEXTURE_1D = 0x0DE0
        /// </summary>
        Texture1D = 3552,
        /// <summary>
        /// Original was GL_TEXTURE_2D = 0x0DE1
        /// </summary>
        Texture2D = 3553,
        /// <summary>
        /// Original was GL_TEXTURE_3D = 0x806F
        /// </summary>
        Texture3D = 32879,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP = 0x8513
        /// </summary>
        TextureCubeMap = 34067,
        /// <summary>
        /// Original was GL_TEXTURE_1D_ARRAY = 0x8C18
        /// </summary>
        Texture1DArray = 35864,
        /// <summary>
        /// Original was GL_TEXTURE_2D_ARRAY = 0x8C1A
        /// </summary>
        Texture2DArray = 35866,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_ARRAY = 0x9009
        /// </summary>
        TextureCubeMapArray = 36873,
        /// <summary>
        /// Original was GL_TEXTURE_2D_MULTISAMPLE = 0x9100
        /// </summary>
        Texture2DMultisample = 37120,
        /// <summary>
        /// Original was GL_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9102
        /// </summary>
        Texture2DMultisampleArray = 37122
    }
}