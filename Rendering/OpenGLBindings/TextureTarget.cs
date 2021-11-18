namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.FramebufferTextureFace, GL.Arb.GetnCompressedTexImage and 96 other functions
    /// </summary>
    public enum TextureTarget
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
        /// Original was GL_PROXY_TEXTURE_1D = 0x8063
        /// </summary>
        ProxyTexture1D = 32867,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_1D_EXT = 0x8063
        /// </summary>
        ProxyTexture1DExt = 32867,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D = 0x8064
        /// </summary>
        ProxyTexture2D = 32868,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_EXT = 0x8064
        /// </summary>
        ProxyTexture2DExt = 32868,
        /// <summary>
        /// Original was GL_TEXTURE_3D = 0x806F
        /// </summary>
        Texture3D = 32879,
        /// <summary>
        /// Original was GL_TEXTURE_3D_EXT = 0x806F
        /// </summary>
        Texture3DExt = 32879,
        /// <summary>
        /// Original was GL_TEXTURE_3D_OES = 0x806F
        /// </summary>
        Texture3DOes = 32879,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_3D = 0x8070
        /// </summary>
        ProxyTexture3D = 32880,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_3D_EXT = 0x8070
        /// </summary>
        ProxyTexture3DExt = 32880,
        /// <summary>
        /// Original was GL_DETAIL_TEXTURE_2D_SGIS = 0x8095
        /// </summary>
        DetailTexture2DSgis = 32917,
        /// <summary>
        /// Original was GL_TEXTURE_4D_SGIS = 0x8134
        /// </summary>
        Texture4DSgis = 33076,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_4D_SGIS = 0x8135
        /// </summary>
        ProxyTexture4DSgis = 33077,
        /// <summary>
        /// Original was GL_TEXTURE_RECTANGLE = 0x84F5
        /// </summary>
        TextureRectangle = 34037,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_RECTANGLE = 0x84F7
        /// </summary>
        ProxyTextureRectangle = 34039,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_RECTANGLE_ARB = 0x84F7
        /// </summary>
        ProxyTextureRectangleArb = 34039,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_RECTANGLE_NV = 0x84F7
        /// </summary>
        ProxyTextureRectangleNv = 34039,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP = 0x8513
        /// </summary>
        TextureCubeMap = 34067,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_CUBE_MAP = 0x8514
        /// </summary>
        TextureBindingCubeMap = 34068,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515
        /// </summary>
        TextureCubeMapPositiveX = 34069,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516
        /// </summary>
        TextureCubeMapNegativeX = 34070,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517
        /// </summary>
        TextureCubeMapPositiveY = 34071,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518
        /// </summary>
        TextureCubeMapNegativeY = 34072,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519
        /// </summary>
        TextureCubeMapPositiveZ = 34073,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A
        /// </summary>
        TextureCubeMapNegativeZ = 34074,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_CUBE_MAP = 0x851B
        /// </summary>
        ProxyTextureCubeMap = 34075,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_CUBE_MAP_ARB = 0x851B
        /// </summary>
        ProxyTextureCubeMapArb = 34075,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_CUBE_MAP_EXT = 0x851B
        /// </summary>
        ProxyTextureCubeMapExt = 34075,
        /// <summary>
        /// Original was GL_TEXTURE_1D_ARRAY = 0x8C18
        /// </summary>
        Texture1DArray = 35864,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_1D_ARRAY = 0x8C19
        /// </summary>
        ProxyTexture1DArray = 35865,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_1D_ARRAY_EXT = 0x8C19
        /// </summary>
        ProxyTexture1DArrayExt = 35865,
        /// <summary>
        /// Original was GL_TEXTURE_2D_ARRAY = 0x8C1A
        /// </summary>
        Texture2DArray = 35866,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_ARRAY = 0x8C1B
        /// </summary>
        ProxyTexture2DArray = 35867,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_ARRAY_EXT = 0x8C1B
        /// </summary>
        ProxyTexture2DArrayExt = 35867,
        /// <summary>
        /// Original was GL_TEXTURE_BUFFER = 0x8C2A
        /// </summary>
        TextureBuffer = 35882,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_ARRAY = 0x9009
        /// </summary>
        TextureCubeMapArray = 36873,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_ARRAY_ARB = 0x9009
        /// </summary>
        TextureCubeMapArrayArb = 36873,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_ARRAY_EXT = 0x9009
        /// </summary>
        TextureCubeMapArrayExt = 36873,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_ARRAY_OES = 0x9009
        /// </summary>
        TextureCubeMapArrayOes = 36873,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_CUBE_MAP_ARRAY = 0x900B
        /// </summary>
        ProxyTextureCubeMapArray = 36875,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_CUBE_MAP_ARRAY_ARB = 0x900B
        /// </summary>
        ProxyTextureCubeMapArrayArb = 36875,
        /// <summary>
        /// Original was GL_TEXTURE_2D_MULTISAMPLE = 0x9100
        /// </summary>
        Texture2DMultisample = 37120,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_MULTISAMPLE = 0x9101
        /// </summary>
        ProxyTexture2DMultisample = 37121,
        /// <summary>
        /// Original was GL_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9102
        /// </summary>
        Texture2DMultisampleArray = 37122,
        /// <summary>
        /// Original was GL_PROXY_TEXTURE_2D_MULTISAMPLE_ARRAY = 0x9103
        /// </summary>
        ProxyTexture2DMultisampleArray = 37123
    }
}