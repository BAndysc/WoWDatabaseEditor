namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.TexBuffer, GL.ColorTable and 32 other functions
    /// </summary>
    public enum InternalFormat
    {
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT = 0x1902
        /// </summary>
        DepthComponent = 6402,
        /// <summary>
        /// Original was GL_RED = 0x1903
        /// </summary>
        Red = 6403,
        /// <summary>
        /// Original was GL_RED_EXT = 0x1903
        /// </summary>
        RedExt = 6403,
        /// <summary>
        /// Original was GL_RGB = 0x1907
        /// </summary>
        Rgb = 6407,
        /// <summary>
        /// Original was GL_RGBA = 0x1908
        /// </summary>
        Rgba = 6408,
        /// <summary>
        /// Original was GL_R3_G3_B2 = 0x2A10
        /// </summary>
        R3G3B2 = 10768,
        /// <summary>
        /// Original was GL_RGB2_EXT = 0x804E
        /// </summary>
        Rgb2Ext = 32846,
        /// <summary>
        /// Original was GL_RGB4 = 0x804F
        /// </summary>
        Rgb4 = 32847,
        /// <summary>
        /// Original was GL_RGB4_EXT = 0x804F
        /// </summary>
        Rgb4Ext = 32847,
        /// <summary>
        /// Original was GL_RGB5 = 0x8050
        /// </summary>
        Rgb5 = 32848,
        /// <summary>
        /// Original was GL_RGB5_EXT = 0x8050
        /// </summary>
        Rgb5Ext = 32848,
        /// <summary>
        /// Original was GL_RGB8 = 0x8051
        /// </summary>
        Rgb8 = 32849,
        /// <summary>
        /// Original was GL_RGB8_EXT = 0x8051
        /// </summary>
        Rgb8Ext = 32849,
        /// <summary>
        /// Original was GL_RGB8_OES = 0x8051
        /// </summary>
        Rgb8Oes = 32849,
        /// <summary>
        /// Original was GL_RGB10 = 0x8052
        /// </summary>
        Rgb10 = 32850,
        /// <summary>
        /// Original was GL_RGB10_EXT = 0x8052
        /// </summary>
        Rgb10Ext = 32850,
        /// <summary>
        /// Original was GL_RGB12 = 0x8053
        /// </summary>
        Rgb12 = 32851,
        /// <summary>
        /// Original was GL_RGB12_EXT = 0x8053
        /// </summary>
        Rgb12Ext = 32851,
        /// <summary>
        /// Original was GL_RGB16 = 0x8054
        /// </summary>
        Rgb16 = 32852,
        /// <summary>
        /// Original was GL_RGB16_EXT = 0x8054
        /// </summary>
        Rgb16Ext = 32852,
        /// <summary>
        /// Original was GL_RGBA4 = 0x8056
        /// </summary>
        Rgba4 = 32854,
        /// <summary>
        /// Original was GL_RGBA4_EXT = 0x8056
        /// </summary>
        Rgba4Ext = 32854,
        /// <summary>
        /// Original was GL_RGBA4_OES = 0x8056
        /// </summary>
        Rgba4Oes = 32854,
        /// <summary>
        /// Original was GL_RGB5_A1 = 0x8057
        /// </summary>
        Rgb5A1 = 32855,
        /// <summary>
        /// Original was GL_RGB5_A1_EXT = 0x8057
        /// </summary>
        Rgb5A1Ext = 32855,
        /// <summary>
        /// Original was GL_RGB5_A1_OES = 0x8057
        /// </summary>
        Rgb5A1Oes = 32855,
        /// <summary>
        /// Original was GL_RGBA8 = 0x8058
        /// </summary>
        Rgba8 = 32856,
        /// <summary>
        /// Original was GL_RGBA8_EXT = 0x8058
        /// </summary>
        Rgba8Ext = 32856,
        /// <summary>
        /// Original was GL_RGBA8_OES = 0x8058
        /// </summary>
        Rgba8Oes = 32856,
        /// <summary>
        /// Original was GL_RGB10_A2 = 0x8059
        /// </summary>
        Rgb10A2 = 32857,
        /// <summary>
        /// Original was GL_RGB10_A2_EXT = 0x8059
        /// </summary>
        Rgb10A2Ext = 32857,
        /// <summary>
        /// Original was GL_RGBA12 = 0x805A
        /// </summary>
        Rgba12 = 32858,
        /// <summary>
        /// Original was GL_RGBA12_EXT = 0x805A
        /// </summary>
        Rgba12Ext = 32858,
        /// <summary>
        /// Original was GL_RGBA16 = 0x805B
        /// </summary>
        Rgba16 = 32859,
        /// <summary>
        /// Original was GL_RGBA16_EXT = 0x805B
        /// </summary>
        Rgba16Ext = 32859,
        /// <summary>
        /// Original was GL_DUAL_ALPHA4_SGIS = 0x8110
        /// </summary>
        DualAlpha4Sgis = 33040,
        /// <summary>
        /// Original was GL_DUAL_ALPHA8_SGIS = 0x8111
        /// </summary>
        DualAlpha8Sgis = 33041,
        /// <summary>
        /// Original was GL_DUAL_ALPHA12_SGIS = 0x8112
        /// </summary>
        DualAlpha12Sgis = 33042,
        /// <summary>
        /// Original was GL_DUAL_ALPHA16_SGIS = 0x8113
        /// </summary>
        DualAlpha16Sgis = 33043,
        /// <summary>
        /// Original was GL_DUAL_LUMINANCE4_SGIS = 0x8114
        /// </summary>
        DualLuminance4Sgis = 33044,
        /// <summary>
        /// Original was GL_DUAL_LUMINANCE8_SGIS = 0x8115
        /// </summary>
        DualLuminance8Sgis = 33045,
        /// <summary>
        /// Original was GL_DUAL_LUMINANCE12_SGIS = 0x8116
        /// </summary>
        DualLuminance12Sgis = 33046,
        /// <summary>
        /// Original was GL_DUAL_LUMINANCE16_SGIS = 0x8117
        /// </summary>
        DualLuminance16Sgis = 33047,
        /// <summary>
        /// Original was GL_DUAL_INTENSITY4_SGIS = 0x8118
        /// </summary>
        DualIntensity4Sgis = 33048,
        /// <summary>
        /// Original was GL_DUAL_INTENSITY8_SGIS = 0x8119
        /// </summary>
        DualIntensity8Sgis = 33049,
        /// <summary>
        /// Original was GL_DUAL_INTENSITY12_SGIS = 0x811A
        /// </summary>
        DualIntensity12Sgis = 33050,
        /// <summary>
        /// Original was GL_DUAL_INTENSITY16_SGIS = 0x811B
        /// </summary>
        DualIntensity16Sgis = 33051,
        /// <summary>
        /// Original was GL_DUAL_LUMINANCE_ALPHA4_SGIS = 0x811C
        /// </summary>
        DualLuminanceAlpha4Sgis = 33052,
        /// <summary>
        /// Original was GL_DUAL_LUMINANCE_ALPHA8_SGIS = 0x811D
        /// </summary>
        DualLuminanceAlpha8Sgis = 33053,
        /// <summary>
        /// Original was GL_QUAD_ALPHA4_SGIS = 0x811E
        /// </summary>
        QuadAlpha4Sgis = 33054,
        /// <summary>
        /// Original was GL_QUAD_ALPHA8_SGIS = 0x811F
        /// </summary>
        QuadAlpha8Sgis = 33055,
        /// <summary>
        /// Original was GL_QUAD_LUMINANCE4_SGIS = 0x8120
        /// </summary>
        QuadLuminance4Sgis = 33056,
        /// <summary>
        /// Original was GL_QUAD_LUMINANCE8_SGIS = 0x8121
        /// </summary>
        QuadLuminance8Sgis = 33057,
        /// <summary>
        /// Original was GL_QUAD_INTENSITY4_SGIS = 0x8122
        /// </summary>
        QuadIntensity4Sgis = 33058,
        /// <summary>
        /// Original was GL_QUAD_INTENSITY8_SGIS = 0x8123
        /// </summary>
        QuadIntensity8Sgis = 33059,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT16 = 0x81A5
        /// </summary>
        DepthComponent16 = 33189,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT16_ARB = 0x81A5
        /// </summary>
        DepthComponent16Arb = 33189,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT16_OES = 0x81A5
        /// </summary>
        DepthComponent16Oes = 33189,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT16_SGIX = 0x81A5
        /// </summary>
        DepthComponent16Sgix = 33189,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT24_ARB = 0x81A6
        /// </summary>
        DepthComponent24Arb = 33190,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT24_OES = 0x81A6
        /// </summary>
        DepthComponent24Oes = 33190,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT24_SGIX = 0x81A6
        /// </summary>
        DepthComponent24Sgix = 33190,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT32_ARB = 0x81A7
        /// </summary>
        DepthComponent32Arb = 33191,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT32_OES = 0x81A7
        /// </summary>
        DepthComponent32Oes = 33191,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT32_SGIX = 0x81A7
        /// </summary>
        DepthComponent32Sgix = 33191,
        /// <summary>
        /// Original was GL_COMPRESSED_RED = 0x8225
        /// </summary>
        CompressedRed = 33317,
        /// <summary>
        /// Original was GL_COMPRESSED_RG = 0x8226
        /// </summary>
        CompressedRg = 33318,
        /// <summary>
        /// Original was GL_RG = 0x8227
        /// </summary>
        Rg = 33319,
        /// <summary>
        /// Original was GL_R8 = 0x8229
        /// </summary>
        R8 = 33321,
        /// <summary>
        /// Original was GL_R8_EXT = 0x8229
        /// </summary>
        R8Ext = 33321,
        /// <summary>
        /// Original was GL_R16 = 0x822A
        /// </summary>
        R16 = 33322,
        /// <summary>
        /// Original was GL_R16_EXT = 0x822A
        /// </summary>
        R16Ext = 33322,
        /// <summary>
        /// Original was GL_RG8 = 0x822B
        /// </summary>
        Rg8 = 33323,
        /// <summary>
        /// Original was GL_RG8_EXT = 0x822B
        /// </summary>
        Rg8Ext = 33323,
        /// <summary>
        /// Original was GL_RG16 = 0x822C
        /// </summary>
        Rg16 = 33324,
        /// <summary>
        /// Original was GL_RG16_EXT = 0x822C
        /// </summary>
        Rg16Ext = 33324,
        /// <summary>
        /// Original was GL_R16F = 0x822D
        /// </summary>
        R16f = 33325,
        /// <summary>
        /// Original was GL_R16F_EXT = 0x822D
        /// </summary>
        R16fExt = 33325,
        /// <summary>
        /// Original was GL_R32F = 0x822E
        /// </summary>
        R32f = 33326,
        /// <summary>
        /// Original was GL_R32F_EXT = 0x822E
        /// </summary>
        R32fExt = 33326,
        /// <summary>
        /// Original was GL_RG16F = 0x822F
        /// </summary>
        Rg16f = 33327,
        /// <summary>
        /// Original was GL_RG16F_EXT = 0x822F
        /// </summary>
        Rg16fExt = 33327,
        /// <summary>
        /// Original was GL_RG32F = 0x8230
        /// </summary>
        Rg32f = 33328,
        /// <summary>
        /// Original was GL_RG32F_EXT = 0x8230
        /// </summary>
        Rg32fExt = 33328,
        /// <summary>
        /// Original was GL_R8I = 0x8231
        /// </summary>
        R8i = 33329,
        /// <summary>
        /// Original was GL_R8UI = 0x8232
        /// </summary>
        R8ui = 33330,
        /// <summary>
        /// Original was GL_R16I = 0x8233
        /// </summary>
        R16i = 33331,
        /// <summary>
        /// Original was GL_R16UI = 0x8234
        /// </summary>
        R16ui = 33332,
        /// <summary>
        /// Original was GL_R32I = 0x8235
        /// </summary>
        R32i = 33333,
        /// <summary>
        /// Original was GL_R32UI = 0x8236
        /// </summary>
        R32ui = 33334,
        /// <summary>
        /// Original was GL_RG8I = 0x8237
        /// </summary>
        Rg8i = 33335,
        /// <summary>
        /// Original was GL_RG8UI = 0x8238
        /// </summary>
        Rg8ui = 33336,
        /// <summary>
        /// Original was GL_RG16I = 0x8239
        /// </summary>
        Rg16i = 33337,
        /// <summary>
        /// Original was GL_RG16UI = 0x823A
        /// </summary>
        Rg16ui = 33338,
        /// <summary>
        /// Original was GL_RG32I = 0x823B
        /// </summary>
        Rg32i = 33339,
        /// <summary>
        /// Original was GL_RG32UI = 0x823C
        /// </summary>
        Rg32ui = 33340,
        /// <summary>
        /// Original was GL_COMPRESSED_RGB_S3TC_DXT1_EXT = 0x83F0
        /// </summary>
        CompressedRgbS3tcDxt1Ext = 33776,
        /// <summary>
        /// Original was GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1
        /// </summary>
        CompressedRgbaS3tcDxt1Ext = 33777,
        /// <summary>
        /// Original was GL_COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2
        /// </summary>
        CompressedRgbaS3tcDxt3Ext = 33778,
        /// <summary>
        /// Original was GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3
        /// </summary>
        CompressedRgbaS3tcDxt5Ext = 33779,
        /// <summary>
        /// Original was GL_COMPRESSED_RGB = 0x84ED
        /// </summary>
        CompressedRgb = 34029,
        /// <summary>
        /// Original was GL_COMPRESSED_RGBA = 0x84EE
        /// </summary>
        CompressedRgba = 34030,
        /// <summary>
        /// Original was GL_DEPTH_STENCIL = 0x84F9
        /// </summary>
        DepthStencil = 34041,
        /// <summary>
        /// Original was GL_DEPTH_STENCIL_EXT = 0x84F9
        /// </summary>
        DepthStencilExt = 34041,
        /// <summary>
        /// Original was GL_DEPTH_STENCIL_NV = 0x84F9
        /// </summary>
        DepthStencilNv = 34041,
        /// <summary>
        /// Original was GL_DEPTH_STENCIL_OES = 0x84F9
        /// </summary>
        DepthStencilOes = 34041,
        /// <summary>
        /// Original was GL_DEPTH_STENCIL_MESA = 0x8750
        /// </summary>
        DepthStencilMesa = 34640,
        /// <summary>
        /// Original was GL_RGBA32F = 0x8814
        /// </summary>
        Rgba32f = 34836,
        /// <summary>
        /// Original was GL_RGBA32F_ARB = 0x8814
        /// </summary>
        Rgba32fArb = 34836,
        /// <summary>
        /// Original was GL_RGBA32F_EXT = 0x8814
        /// </summary>
        Rgba32fExt = 34836,
        /// <summary>
        /// Original was GL_RGBA16F = 0x881A
        /// </summary>
        Rgba16f = 34842,
        /// <summary>
        /// Original was GL_RGBA16F_ARB = 0x881A
        /// </summary>
        Rgba16fArb = 34842,
        /// <summary>
        /// Original was GL_RGBA16F_EXT = 0x881A
        /// </summary>
        Rgba16fExt = 34842,
        /// <summary>
        /// Original was GL_RGB16F = 0x881B
        /// </summary>
        Rgb16f = 34843,
        /// <summary>
        /// Original was GL_RGB16F_ARB = 0x881B
        /// </summary>
        Rgb16fArb = 34843,
        /// <summary>
        /// Original was GL_RGB16F_EXT = 0x881B
        /// </summary>
        Rgb16fExt = 34843,
        /// <summary>
        /// Original was GL_DEPTH24_STENCIL8 = 0x88F0
        /// </summary>
        Depth24Stencil8 = 35056,
        /// <summary>
        /// Original was GL_DEPTH24_STENCIL8_EXT = 0x88F0
        /// </summary>
        Depth24Stencil8Ext = 35056,
        /// <summary>
        /// Original was GL_DEPTH24_STENCIL8_OES = 0x88F0
        /// </summary>
        Depth24Stencil8Oes = 35056,
        /// <summary>
        /// Original was GL_R11F_G11F_B10F = 0x8C3A
        /// </summary>
        R11fG11fB10f = 35898,
        /// <summary>
        /// Original was GL_R11F_G11F_B10F_APPLE = 0x8C3A
        /// </summary>
        R11fG11fB10fApple = 35898,
        /// <summary>
        /// Original was GL_R11F_G11F_B10F_EXT = 0x8C3A
        /// </summary>
        R11fG11fB10fExt = 35898,
        /// <summary>
        /// Original was GL_RGB9_E5 = 0x8C3D
        /// </summary>
        Rgb9E5 = 35901,
        /// <summary>
        /// Original was GL_RGB9_E5_APPLE = 0x8C3D
        /// </summary>
        Rgb9E5Apple = 35901,
        /// <summary>
        /// Original was GL_RGB9_E5_EXT = 0x8C3D
        /// </summary>
        Rgb9E5Ext = 35901,
        /// <summary>
        /// Original was GL_SRGB = 0x8C40
        /// </summary>
        Srgb = 35904,
        /// <summary>
        /// Original was GL_SRGB_EXT = 0x8C40
        /// </summary>
        SrgbExt = 35904,
        /// <summary>
        /// Original was GL_SRGB8 = 0x8C41
        /// </summary>
        Srgb8 = 35905,
        /// <summary>
        /// Original was GL_SRGB8_EXT = 0x8C41
        /// </summary>
        Srgb8Ext = 35905,
        /// <summary>
        /// Original was GL_SRGB8_NV = 0x8C41
        /// </summary>
        Srgb8Nv = 35905,
        /// <summary>
        /// Original was GL_SRGB_ALPHA = 0x8C42
        /// </summary>
        SrgbAlpha = 35906,
        /// <summary>
        /// Original was GL_SRGB_ALPHA_EXT = 0x8C42
        /// </summary>
        SrgbAlphaExt = 35906,
        /// <summary>
        /// Original was GL_SRGB8_ALPHA8 = 0x8C43
        /// </summary>
        Srgb8Alpha8 = 35907,
        /// <summary>
        /// Original was GL_SRGB8_ALPHA8_EXT = 0x8C43
        /// </summary>
        Srgb8Alpha8Ext = 35907,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB = 0x8C48
        /// </summary>
        CompressedSrgb = 35912,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB_ALPHA = 0x8C49
        /// </summary>
        CompressedSrgbAlpha = 35913,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB_S3TC_DXT1_EXT = 0x8C4C
        /// </summary>
        CompressedSrgbS3tcDxt1Ext = 35916,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT1_EXT = 0x8C4D
        /// </summary>
        CompressedSrgbAlphaS3tcDxt1Ext = 35917,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT3_EXT = 0x8C4E
        /// </summary>
        CompressedSrgbAlphaS3tcDxt3Ext = 35918,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT = 0x8C4F
        /// </summary>
        CompressedSrgbAlphaS3tcDxt5Ext = 35919,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT32F = 0x8CAC
        /// </summary>
        DepthComponent32f = 36012,
        /// <summary>
        /// Original was GL_DEPTH32F_STENCIL8 = 0x8CAD
        /// </summary>
        Depth32fStencil8 = 36013,
        /// <summary>
        /// Original was GL_RGBA32UI = 0x8D70
        /// </summary>
        Rgba32ui = 36208,
        /// <summary>
        /// Original was GL_RGB32UI = 0x8D71
        /// </summary>
        Rgb32ui = 36209,
        /// <summary>
        /// Original was GL_RGBA16UI = 0x8D76
        /// </summary>
        Rgba16ui = 36214,
        /// <summary>
        /// Original was GL_RGB16UI = 0x8D77
        /// </summary>
        Rgb16ui = 36215,
        /// <summary>
        /// Original was GL_RGBA8UI = 0x8D7C
        /// </summary>
        Rgba8ui = 36220,
        /// <summary>
        /// Original was GL_RGB8UI = 0x8D7D
        /// </summary>
        Rgb8ui = 36221,
        /// <summary>
        /// Original was GL_RGBA32I = 0x8D82
        /// </summary>
        Rgba32i = 36226,
        /// <summary>
        /// Original was GL_RGB32I = 0x8D83
        /// </summary>
        Rgb32i = 36227,
        /// <summary>
        /// Original was GL_RGBA16I = 0x8D88
        /// </summary>
        Rgba16i = 36232,
        /// <summary>
        /// Original was GL_RGB16I = 0x8D89
        /// </summary>
        Rgb16i = 36233,
        /// <summary>
        /// Original was GL_RGBA8I = 0x8D8E
        /// </summary>
        Rgba8i = 36238,
        /// <summary>
        /// Original was GL_RGB8I = 0x8D8F
        /// </summary>
        Rgb8i = 36239,
        /// <summary>
        /// Original was GL_DEPTH_COMPONENT32F_NV = 0x8DAB
        /// </summary>
        DepthComponent32fNv = 36267,
        /// <summary>
        /// Original was GL_DEPTH32F_STENCIL8_NV = 0x8DAC
        /// </summary>
        Depth32fStencil8Nv = 36268,
        /// <summary>
        /// Original was GL_COMPRESSED_RED_RGTC1 = 0x8DBB
        /// </summary>
        CompressedRedRgtc1 = 36283,
        /// <summary>
        /// Original was GL_COMPRESSED_RED_RGTC1_EXT = 0x8DBB
        /// </summary>
        CompressedRedRgtc1Ext = 36283,
        /// <summary>
        /// Original was GL_COMPRESSED_SIGNED_RED_RGTC1 = 0x8DBC
        /// </summary>
        CompressedSignedRedRgtc1 = 36284,
        /// <summary>
        /// Original was GL_COMPRESSED_SIGNED_RED_RGTC1_EXT = 0x8DBC
        /// </summary>
        CompressedSignedRedRgtc1Ext = 36284,
        /// <summary>
        /// Original was GL_COMPRESSED_RG_RGTC2 = 0x8DBD
        /// </summary>
        CompressedRgRgtc2 = 36285,
        /// <summary>
        /// Original was GL_COMPRESSED_SIGNED_RG_RGTC2 = 0x8DBE
        /// </summary>
        CompressedSignedRgRgtc2 = 36286,
        /// <summary>
        /// Original was GL_COMPRESSED_RGBA_BPTC_UNORM = 0x8E8C
        /// </summary>
        CompressedRgbaBptcUnorm = 36492,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM = 0x8E8D
        /// </summary>
        CompressedSrgbAlphaBptcUnorm = 36493,
        /// <summary>
        /// Original was GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT = 0x8E8E
        /// </summary>
        CompressedRgbBptcSignedFloat = 36494,
        /// <summary>
        /// Original was GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT = 0x8E8F
        /// </summary>
        CompressedRgbBptcUnsignedFloat = 36495,
        /// <summary>
        /// Original was GL_R8_SNORM = 0x8F94
        /// </summary>
        R8Snorm = 36756,
        /// <summary>
        /// Original was GL_RG8_SNORM = 0x8F95
        /// </summary>
        Rg8Snorm = 36757,
        /// <summary>
        /// Original was GL_RGB8_SNORM = 0x8F96
        /// </summary>
        Rgb8Snorm = 36758,
        /// <summary>
        /// Original was GL_RGBA8_SNORM = 0x8F97
        /// </summary>
        Rgba8Snorm = 36759,
        /// <summary>
        /// Original was GL_R16_SNORM = 0x8F98
        /// </summary>
        R16Snorm = 36760,
        /// <summary>
        /// Original was GL_R16_SNORM_EXT = 0x8F98
        /// </summary>
        R16SnormExt = 36760,
        /// <summary>
        /// Original was GL_RG16_SNORM = 0x8F99
        /// </summary>
        Rg16Snorm = 36761,
        /// <summary>
        /// Original was GL_RG16_SNORM_EXT = 0x8F99
        /// </summary>
        Rg16SnormExt = 36761,
        /// <summary>
        /// Original was GL_RGB16_SNORM = 0x8F9A
        /// </summary>
        Rgb16Snorm = 36762,
        /// <summary>
        /// Original was GL_RGB16_SNORM_EXT = 0x8F9A
        /// </summary>
        Rgb16SnormExt = 36762,
        /// <summary>
        /// Original was GL_RGB10_A2UI = 0x906F
        /// </summary>
        Rgb10A2ui = 36975,
        /// <summary>
        /// Original was GL_COMPRESSED_R11_EAC = 0x9270
        /// </summary>
        CompressedR11Eac = 37488,
        /// <summary>
        /// Original was GL_COMPRESSED_SIGNED_R11_EAC = 0x9271
        /// </summary>
        CompressedSignedR11Eac = 37489,
        /// <summary>
        /// Original was GL_COMPRESSED_RG11_EAC = 0x9272
        /// </summary>
        CompressedRg11Eac = 37490,
        /// <summary>
        /// Original was GL_COMPRESSED_SIGNED_RG11_EAC = 0x9273
        /// </summary>
        CompressedSignedRg11Eac = 37491,
        /// <summary>
        /// Original was GL_COMPRESSED_RGB8_ETC2 = 0x9274
        /// </summary>
        CompressedRgb8Etc2 = 37492,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB8_ETC2 = 0x9275
        /// </summary>
        CompressedSrgb8Etc2 = 37493,
        /// <summary>
        /// Original was GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9276
        /// </summary>
        CompressedRgb8PunchthroughAlpha1Etc2 = 37494,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2 = 0x9277
        /// </summary>
        CompressedSrgb8PunchthroughAlpha1Etc2 = 37495,
        /// <summary>
        /// Original was GL_COMPRESSED_RGBA8_ETC2_EAC = 0x9278
        /// </summary>
        CompressedRgba8Etc2Eac = 37496,
        /// <summary>
        /// Original was GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC = 0x9279
        /// </summary>
        CompressedSrgb8Alpha8Etc2Eac = 37497
    }
}