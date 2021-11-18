namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GetBoolean, GL.GetDouble and 3 other functions
    /// </summary>
    public enum GetPName
    {
        /// <summary>
        /// Original was GL_POINT_SMOOTH = 0x0B10
        /// </summary>
        PointSmooth = 2832,
        /// <summary>
        /// Original was GL_POINT_SIZE = 0x0B11
        /// </summary>
        PointSize = 2833,
        /// <summary>
        /// Original was GL_POINT_SIZE_RANGE = 0x0B12
        /// </summary>
        PointSizeRange = 2834,
        /// <summary>
        /// Original was GL_SMOOTH_POINT_SIZE_RANGE = 0x0B12
        /// </summary>
        SmoothPointSizeRange = 2834,
        /// <summary>
        /// Original was GL_POINT_SIZE_GRANULARITY = 0x0B13
        /// </summary>
        PointSizeGranularity = 2835,
        /// <summary>
        /// Original was GL_SMOOTH_POINT_SIZE_GRANULARITY = 0x0B13
        /// </summary>
        SmoothPointSizeGranularity = 2835,
        /// <summary>
        /// Original was GL_LINE_SMOOTH = 0x0B20
        /// </summary>
        LineSmooth = 2848,
        /// <summary>
        /// Original was GL_LINE_WIDTH = 0x0B21
        /// </summary>
        LineWidth = 2849,
        /// <summary>
        /// Original was GL_LINE_WIDTH_RANGE = 0x0B22
        /// </summary>
        LineWidthRange = 2850,
        /// <summary>
        /// Original was GL_SMOOTH_LINE_WIDTH_RANGE = 0x0B22
        /// </summary>
        SmoothLineWidthRange = 2850,
        /// <summary>
        /// Original was GL_LINE_WIDTH_GRANULARITY = 0x0B23
        /// </summary>
        LineWidthGranularity = 2851,
        /// <summary>
        /// Original was GL_SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23
        /// </summary>
        SmoothLineWidthGranularity = 2851,
        /// <summary>
        /// Original was GL_LINE_STIPPLE = 0x0B24
        /// </summary>
        LineStipple = 2852,
        /// <summary>
        /// Original was GL_POLYGON_MODE = 0x0B40
        /// </summary>
        PolygonMode = 2880,
        /// <summary>
        /// Original was GL_POLYGON_SMOOTH = 0x0B41
        /// </summary>
        PolygonSmooth = 2881,
        /// <summary>
        /// Original was GL_POLYGON_STIPPLE = 0x0B42
        /// </summary>
        PolygonStipple = 2882,
        /// <summary>
        /// Original was GL_CULL_FACE = 0x0B44
        /// </summary>
        CullFace = 2884,
        /// <summary>
        /// Original was GL_CULL_FACE_MODE = 0x0B45
        /// </summary>
        CullFaceMode = 2885,
        /// <summary>
        /// Original was GL_FRONT_FACE = 0x0B46
        /// </summary>
        FrontFace = 2886,
        /// <summary>
        /// Original was GL_LIGHTING = 0x0B50
        /// </summary>
        Lighting = 2896,
        /// <summary>
        /// Original was GL_COLOR_MATERIAL = 0x0B57
        /// </summary>
        ColorMaterial = 2903,
        /// <summary>
        /// Original was GL_FOG = 0x0B60
        /// </summary>
        Fog = 2912,
        /// <summary>
        /// Original was GL_FOG_INDEX = 0x0B61
        /// </summary>
        FogIndex = 2913,
        /// <summary>
        /// Original was GL_FOG_DENSITY = 0x0B62
        /// </summary>
        FogDensity = 2914,
        /// <summary>
        /// Original was GL_FOG_START = 0x0B63
        /// </summary>
        FogStart = 2915,
        /// <summary>
        /// Original was GL_FOG_END = 0x0B64
        /// </summary>
        FogEnd = 2916,
        /// <summary>
        /// Original was GL_FOG_MODE = 0x0B65
        /// </summary>
        FogMode = 2917,
        /// <summary>
        /// Original was GL_FOG_COLOR = 0x0B66
        /// </summary>
        FogColor = 2918,
        /// <summary>
        /// Original was GL_DEPTH_RANGE = 0x0B70
        /// </summary>
        DepthRange = 2928,
        /// <summary>
        /// Original was GL_DEPTH_TEST = 0x0B71
        /// </summary>
        DepthTest = 2929,
        /// <summary>
        /// Original was GL_DEPTH_WRITEMASK = 0x0B72
        /// </summary>
        DepthWritemask = 2930,
        /// <summary>
        /// Original was GL_DEPTH_CLEAR_VALUE = 0x0B73
        /// </summary>
        DepthClearValue = 2931,
        /// <summary>
        /// Original was GL_DEPTH_FUNC = 0x0B74
        /// </summary>
        DepthFunc = 2932,
        /// <summary>
        /// Original was GL_STENCIL_TEST = 0x0B90
        /// </summary>
        StencilTest = 2960,
        /// <summary>
        /// Original was GL_STENCIL_CLEAR_VALUE = 0x0B91
        /// </summary>
        StencilClearValue = 2961,
        /// <summary>
        /// Original was GL_STENCIL_FUNC = 0x0B92
        /// </summary>
        StencilFunc = 2962,
        /// <summary>
        /// Original was GL_STENCIL_VALUE_MASK = 0x0B93
        /// </summary>
        StencilValueMask = 2963,
        /// <summary>
        /// Original was GL_STENCIL_FAIL = 0x0B94
        /// </summary>
        StencilFail = 2964,
        /// <summary>
        /// Original was GL_STENCIL_PASS_DEPTH_FAIL = 0x0B95
        /// </summary>
        StencilPassDepthFail = 2965,
        /// <summary>
        /// Original was GL_STENCIL_PASS_DEPTH_PASS = 0x0B96
        /// </summary>
        StencilPassDepthPass = 2966,
        /// <summary>
        /// Original was GL_STENCIL_REF = 0x0B97
        /// </summary>
        StencilRef = 2967,
        /// <summary>
        /// Original was GL_STENCIL_WRITEMASK = 0x0B98
        /// </summary>
        StencilWritemask = 2968,
        /// <summary>
        /// Original was GL_NORMALIZE = 0x0BA1
        /// </summary>
        Normalize = 2977,
        /// <summary>
        /// Original was GL_VIEWPORT = 0x0BA2
        /// </summary>
        Viewport = 2978,
        /// <summary>
        /// Original was GL_MODELVIEW0_STACK_DEPTH_EXT = 0x0BA3
        /// </summary>
        Modelview0StackDepthExt = 2979,
        /// <summary>
        /// Original was GL_MODELVIEW0_MATRIX_EXT = 0x0BA6
        /// </summary>
        Modelview0MatrixExt = 2982,
        /// <summary>
        /// Original was GL_ALPHA_TEST = 0x0BC0
        /// </summary>
        AlphaTest = 3008,
        /// <summary>
        /// Original was GL_ALPHA_TEST_QCOM = 0x0BC0
        /// </summary>
        AlphaTestQcom = 3008,
        /// <summary>
        /// Original was GL_ALPHA_TEST_FUNC_QCOM = 0x0BC1
        /// </summary>
        AlphaTestFuncQcom = 3009,
        /// <summary>
        /// Original was GL_ALPHA_TEST_REF_QCOM = 0x0BC2
        /// </summary>
        AlphaTestRefQcom = 3010,
        /// <summary>
        /// Original was GL_DITHER = 0x0BD0
        /// </summary>
        Dither = 3024,
        /// <summary>
        /// Original was GL_BLEND_DST = 0x0BE0
        /// </summary>
        BlendDst = 3040,
        /// <summary>
        /// Original was GL_BLEND_SRC = 0x0BE1
        /// </summary>
        BlendSrc = 3041,
        /// <summary>
        /// Original was GL_BLEND = 0x0BE2
        /// </summary>
        Blend = 3042,
        /// <summary>
        /// Original was GL_LOGIC_OP_MODE = 0x0BF0
        /// </summary>
        LogicOpMode = 3056,
        /// <summary>
        /// Original was GL_INDEX_LOGIC_OP = 0x0BF1
        /// </summary>
        IndexLogicOp = 3057,
        /// <summary>
        /// Original was GL_LOGIC_OP = 0x0BF1
        /// </summary>
        LogicOp = 3057,
        /// <summary>
        /// Original was GL_COLOR_LOGIC_OP = 0x0BF2
        /// </summary>
        ColorLogicOp = 3058,
        /// <summary>
        /// Original was GL_DRAW_BUFFER = 0x0C01
        /// </summary>
        DrawBuffer = 3073,
        /// <summary>
        /// Original was GL_DRAW_BUFFER_EXT = 0x0C01
        /// </summary>
        DrawBufferExt = 3073,
        /// <summary>
        /// Original was GL_READ_BUFFER = 0x0C02
        /// </summary>
        ReadBuffer = 3074,
        /// <summary>
        /// Original was GL_READ_BUFFER_EXT = 0x0C02
        /// </summary>
        ReadBufferExt = 3074,
        /// <summary>
        /// Original was GL_READ_BUFFER_NV = 0x0C02
        /// </summary>
        ReadBufferNv = 3074,
        /// <summary>
        /// Original was GL_SCISSOR_BOX = 0x0C10
        /// </summary>
        ScissorBox = 3088,
        /// <summary>
        /// Original was GL_SCISSOR_TEST = 0x0C11
        /// </summary>
        ScissorTest = 3089,
        /// <summary>
        /// Original was GL_COLOR_CLEAR_VALUE = 0x0C22
        /// </summary>
        ColorClearValue = 3106,
        /// <summary>
        /// Original was GL_COLOR_WRITEMASK = 0x0C23
        /// </summary>
        ColorWritemask = 3107,
        /// <summary>
        /// Original was GL_DOUBLEBUFFER = 0x0C32
        /// </summary>
        Doublebuffer = 3122,
        /// <summary>
        /// Original was GL_STEREO = 0x0C33
        /// </summary>
        Stereo = 3123,
        /// <summary>
        /// Original was GL_LINE_SMOOTH_HINT = 0x0C52
        /// </summary>
        LineSmoothHint = 3154,
        /// <summary>
        /// Original was GL_POLYGON_SMOOTH_HINT = 0x0C53
        /// </summary>
        PolygonSmoothHint = 3155,
        /// <summary>
        /// Original was GL_TEXTURE_GEN_S = 0x0C60
        /// </summary>
        TextureGenS = 3168,
        /// <summary>
        /// Original was GL_TEXTURE_GEN_T = 0x0C61
        /// </summary>
        TextureGenT = 3169,
        /// <summary>
        /// Original was GL_TEXTURE_GEN_R = 0x0C62
        /// </summary>
        TextureGenR = 3170,
        /// <summary>
        /// Original was GL_TEXTURE_GEN_Q = 0x0C63
        /// </summary>
        TextureGenQ = 3171,
        /// <summary>
        /// Original was GL_UNPACK_SWAP_BYTES = 0x0CF0
        /// </summary>
        UnpackSwapBytes = 3312,
        /// <summary>
        /// Original was GL_UNPACK_LSB_FIRST = 0x0CF1
        /// </summary>
        UnpackLsbFirst = 3313,
        /// <summary>
        /// Original was GL_UNPACK_ROW_LENGTH = 0x0CF2
        /// </summary>
        UnpackRowLength = 3314,
        /// <summary>
        /// Original was GL_UNPACK_SKIP_ROWS = 0x0CF3
        /// </summary>
        UnpackSkipRows = 3315,
        /// <summary>
        /// Original was GL_UNPACK_SKIP_PIXELS = 0x0CF4
        /// </summary>
        UnpackSkipPixels = 3316,
        /// <summary>
        /// Original was GL_UNPACK_ALIGNMENT = 0x0CF5
        /// </summary>
        UnpackAlignment = 3317,
        /// <summary>
        /// Original was GL_PACK_SWAP_BYTES = 0x0D00
        /// </summary>
        PackSwapBytes = 3328,
        /// <summary>
        /// Original was GL_PACK_LSB_FIRST = 0x0D01
        /// </summary>
        PackLsbFirst = 3329,
        /// <summary>
        /// Original was GL_PACK_ROW_LENGTH = 0x0D02
        /// </summary>
        PackRowLength = 3330,
        /// <summary>
        /// Original was GL_PACK_SKIP_ROWS = 0x0D03
        /// </summary>
        PackSkipRows = 3331,
        /// <summary>
        /// Original was GL_PACK_SKIP_PIXELS = 0x0D04
        /// </summary>
        PackSkipPixels = 3332,
        /// <summary>
        /// Original was GL_PACK_ALIGNMENT = 0x0D05
        /// </summary>
        PackAlignment = 3333,
        /// <summary>
        /// Original was GL_MAX_CLIP_DISTANCES = 0x0D32
        /// </summary>
        MaxClipDistances = 3378,
        /// <summary>
        /// Original was GL_MAX_TEXTURE_SIZE = 0x0D33
        /// </summary>
        MaxTextureSize = 3379,
        /// <summary>
        /// Original was GL_MAX_VIEWPORT_DIMS = 0x0D3A
        /// </summary>
        MaxViewportDims = 3386,
        /// <summary>
        /// Original was GL_SUBPIXEL_BITS = 0x0D50
        /// </summary>
        SubpixelBits = 3408,
        /// <summary>
        /// Original was GL_AUTO_NORMAL = 0x0D80
        /// </summary>
        AutoNormal = 3456,
        /// <summary>
        /// Original was GL_MAP1_COLOR_4 = 0x0D90
        /// </summary>
        Map1Color4 = 3472,
        /// <summary>
        /// Original was GL_MAP1_INDEX = 0x0D91
        /// </summary>
        Map1Index = 3473,
        /// <summary>
        /// Original was GL_MAP1_NORMAL = 0x0D92
        /// </summary>
        Map1Normal = 3474,
        /// <summary>
        /// Original was GL_MAP1_TEXTURE_COORD_1 = 0x0D93
        /// </summary>
        Map1TextureCoord1 = 3475,
        /// <summary>
        /// Original was GL_MAP1_TEXTURE_COORD_2 = 0x0D94
        /// </summary>
        Map1TextureCoord2 = 3476,
        /// <summary>
        /// Original was GL_MAP1_TEXTURE_COORD_3 = 0x0D95
        /// </summary>
        Map1TextureCoord3 = 3477,
        /// <summary>
        /// Original was GL_MAP1_TEXTURE_COORD_4 = 0x0D96
        /// </summary>
        Map1TextureCoord4 = 3478,
        /// <summary>
        /// Original was GL_MAP1_VERTEX_3 = 0x0D97
        /// </summary>
        Map1Vertex3 = 3479,
        /// <summary>
        /// Original was GL_MAP1_VERTEX_4 = 0x0D98
        /// </summary>
        Map1Vertex4 = 3480,
        /// <summary>
        /// Original was GL_MAP2_COLOR_4 = 0x0DB0
        /// </summary>
        Map2Color4 = 3504,
        /// <summary>
        /// Original was GL_MAP2_INDEX = 0x0DB1
        /// </summary>
        Map2Index = 3505,
        /// <summary>
        /// Original was GL_MAP2_NORMAL = 0x0DB2
        /// </summary>
        Map2Normal = 3506,
        /// <summary>
        /// Original was GL_MAP2_TEXTURE_COORD_1 = 0x0DB3
        /// </summary>
        Map2TextureCoord1 = 3507,
        /// <summary>
        /// Original was GL_MAP2_TEXTURE_COORD_2 = 0x0DB4
        /// </summary>
        Map2TextureCoord2 = 3508,
        /// <summary>
        /// Original was GL_MAP2_TEXTURE_COORD_3 = 0x0DB5
        /// </summary>
        Map2TextureCoord3 = 3509,
        /// <summary>
        /// Original was GL_MAP2_TEXTURE_COORD_4 = 0x0DB6
        /// </summary>
        Map2TextureCoord4 = 3510,
        /// <summary>
        /// Original was GL_MAP2_VERTEX_3 = 0x0DB7
        /// </summary>
        Map2Vertex3 = 3511,
        /// <summary>
        /// Original was GL_MAP2_VERTEX_4 = 0x0DB8
        /// </summary>
        Map2Vertex4 = 3512,
        /// <summary>
        /// Original was GL_TEXTURE_1D = 0x0DE0
        /// </summary>
        Texture1D = 3552,
        /// <summary>
        /// Original was GL_TEXTURE_2D = 0x0DE1
        /// </summary>
        Texture2D = 3553,
        /// <summary>
        /// Original was GL_POLYGON_OFFSET_UNITS = 0x2A00
        /// </summary>
        PolygonOffsetUnits = 10752,
        /// <summary>
        /// Original was GL_POLYGON_OFFSET_POINT = 0x2A01
        /// </summary>
        PolygonOffsetPoint = 10753,
        /// <summary>
        /// Original was GL_POLYGON_OFFSET_LINE = 0x2A02
        /// </summary>
        PolygonOffsetLine = 10754,
        /// <summary>
        /// Original was GL_CLIP_PLANE0 = 0x3000
        /// </summary>
        ClipPlane0 = 12288,
        /// <summary>
        /// Original was GL_CLIP_PLANE1 = 0x3001
        /// </summary>
        ClipPlane1 = 12289,
        /// <summary>
        /// Original was GL_CLIP_PLANE2 = 0x3002
        /// </summary>
        ClipPlane2 = 12290,
        /// <summary>
        /// Original was GL_CLIP_PLANE3 = 0x3003
        /// </summary>
        ClipPlane3 = 12291,
        /// <summary>
        /// Original was GL_CLIP_PLANE4 = 0x3004
        /// </summary>
        ClipPlane4 = 12292,
        /// <summary>
        /// Original was GL_CLIP_PLANE5 = 0x3005
        /// </summary>
        ClipPlane5 = 12293,
        /// <summary>
        /// Original was GL_LIGHT0 = 0x4000
        /// </summary>
        Light0 = 0x4000,
        /// <summary>
        /// Original was GL_LIGHT1 = 0x4001
        /// </summary>
        Light1 = 16385,
        /// <summary>
        /// Original was GL_LIGHT2 = 0x4002
        /// </summary>
        Light2 = 16386,
        /// <summary>
        /// Original was GL_LIGHT3 = 0x4003
        /// </summary>
        Light3 = 16387,
        /// <summary>
        /// Original was GL_LIGHT4 = 0x4004
        /// </summary>
        Light4 = 16388,
        /// <summary>
        /// Original was GL_LIGHT5 = 0x4005
        /// </summary>
        Light5 = 16389,
        /// <summary>
        /// Original was GL_LIGHT6 = 0x4006
        /// </summary>
        Light6 = 16390,
        /// <summary>
        /// Original was GL_LIGHT7 = 0x4007
        /// </summary>
        Light7 = 16391,
        /// <summary>
        /// Original was GL_BLEND_COLOR_EXT = 0x8005
        /// </summary>
        BlendColorExt = 32773,
        /// <summary>
        /// Original was GL_BLEND_EQUATION_EXT = 0x8009
        /// </summary>
        BlendEquationExt = 32777,
        /// <summary>
        /// Original was GL_BLEND_EQUATION_RGB = 0x8009
        /// </summary>
        BlendEquationRgb = 32777,
        /// <summary>
        /// Original was GL_PACK_CMYK_HINT_EXT = 0x800E
        /// </summary>
        PackCmykHintExt = 32782,
        /// <summary>
        /// Original was GL_UNPACK_CMYK_HINT_EXT = 0x800F
        /// </summary>
        UnpackCmykHintExt = 32783,
        /// <summary>
        /// Original was GL_CONVOLUTION_1D_EXT = 0x8010
        /// </summary>
        Convolution1DExt = 32784,
        /// <summary>
        /// Original was GL_CONVOLUTION_2D_EXT = 0x8011
        /// </summary>
        Convolution2DExt = 32785,
        /// <summary>
        /// Original was GL_SEPARABLE_2D_EXT = 0x8012
        /// </summary>
        Separable2DExt = 32786,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_RED_SCALE_EXT = 0x801C
        /// </summary>
        PostConvolutionRedScaleExt = 32796,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_GREEN_SCALE_EXT = 0x801D
        /// </summary>
        PostConvolutionGreenScaleExt = 32797,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_BLUE_SCALE_EXT = 0x801E
        /// </summary>
        PostConvolutionBlueScaleExt = 32798,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_ALPHA_SCALE_EXT = 0x801F
        /// </summary>
        PostConvolutionAlphaScaleExt = 32799,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_RED_BIAS_EXT = 0x8020
        /// </summary>
        PostConvolutionRedBiasExt = 32800,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_GREEN_BIAS_EXT = 0x8021
        /// </summary>
        PostConvolutionGreenBiasExt = 32801,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_BLUE_BIAS_EXT = 0x8022
        /// </summary>
        PostConvolutionBlueBiasExt = 32802,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_ALPHA_BIAS_EXT = 0x8023
        /// </summary>
        PostConvolutionAlphaBiasExt = 32803,
        /// <summary>
        /// Original was GL_HISTOGRAM_EXT = 0x8024
        /// </summary>
        HistogramExt = 32804,
        /// <summary>
        /// Original was GL_MINMAX_EXT = 0x802E
        /// </summary>
        MinmaxExt = 32814,
        /// <summary>
        /// Original was GL_POLYGON_OFFSET_FILL = 0x8037
        /// </summary>
        PolygonOffsetFill = 32823,
        /// <summary>
        /// Original was GL_POLYGON_OFFSET_FACTOR = 0x8038
        /// </summary>
        PolygonOffsetFactor = 32824,
        /// <summary>
        /// Original was GL_POLYGON_OFFSET_BIAS_EXT = 0x8039
        /// </summary>
        PolygonOffsetBiasExt = 32825,
        /// <summary>
        /// Original was GL_RESCALE_NORMAL_EXT = 0x803A
        /// </summary>
        RescaleNormalExt = 32826,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_1D = 0x8068
        /// </summary>
        TextureBinding1D = 32872,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_2D = 0x8069
        /// </summary>
        TextureBinding2D = 32873,
        /// <summary>
        /// Original was GL_TEXTURE_3D_BINDING_EXT = 0x806A
        /// </summary>
        Texture3DBindingExt = 32874,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_3D = 0x806A
        /// </summary>
        TextureBinding3D = 32874,
        /// <summary>
        /// Original was GL_PACK_SKIP_IMAGES_EXT = 0x806B
        /// </summary>
        PackSkipImagesExt = 32875,
        /// <summary>
        /// Original was GL_PACK_IMAGE_HEIGHT_EXT = 0x806C
        /// </summary>
        PackImageHeightExt = 32876,
        /// <summary>
        /// Original was GL_UNPACK_SKIP_IMAGES_EXT = 0x806D
        /// </summary>
        UnpackSkipImagesExt = 32877,
        /// <summary>
        /// Original was GL_UNPACK_IMAGE_HEIGHT_EXT = 0x806E
        /// </summary>
        UnpackImageHeightExt = 32878,
        /// <summary>
        /// Original was GL_TEXTURE_3D_EXT = 0x806F
        /// </summary>
        Texture3DExt = 32879,
        /// <summary>
        /// Original was GL_MAX_3D_TEXTURE_SIZE = 0x8073
        /// </summary>
        Max3DTextureSize = 32883,
        /// <summary>
        /// Original was GL_MAX_3D_TEXTURE_SIZE_EXT = 0x8073
        /// </summary>
        Max3DTextureSizeExt = 32883,
        /// <summary>
        /// Original was GL_VERTEX_ARRAY = 0x8074
        /// </summary>
        VertexArray = 32884,
        /// <summary>
        /// Original was GL_NORMAL_ARRAY = 0x8075
        /// </summary>
        NormalArray = 32885,
        /// <summary>
        /// Original was GL_COLOR_ARRAY = 0x8076
        /// </summary>
        ColorArray = 32886,
        /// <summary>
        /// Original was GL_INDEX_ARRAY = 0x8077
        /// </summary>
        IndexArray = 32887,
        /// <summary>
        /// Original was GL_TEXTURE_COORD_ARRAY = 0x8078
        /// </summary>
        TextureCoordArray = 32888,
        /// <summary>
        /// Original was GL_EDGE_FLAG_ARRAY = 0x8079
        /// </summary>
        EdgeFlagArray = 32889,
        /// <summary>
        /// Original was GL_VERTEX_ARRAY_COUNT_EXT = 0x807D
        /// </summary>
        VertexArrayCountExt = 32893,
        /// <summary>
        /// Original was GL_NORMAL_ARRAY_COUNT_EXT = 0x8080
        /// </summary>
        NormalArrayCountExt = 32896,
        /// <summary>
        /// Original was GL_COLOR_ARRAY_COUNT_EXT = 0x8084
        /// </summary>
        ColorArrayCountExt = 32900,
        /// <summary>
        /// Original was GL_INDEX_ARRAY_COUNT_EXT = 0x8087
        /// </summary>
        IndexArrayCountExt = 32903,
        /// <summary>
        /// Original was GL_TEXTURE_COORD_ARRAY_COUNT_EXT = 0x808B
        /// </summary>
        TextureCoordArrayCountExt = 32907,
        /// <summary>
        /// Original was GL_EDGE_FLAG_ARRAY_COUNT_EXT = 0x808D
        /// </summary>
        EdgeFlagArrayCountExt = 32909,
        /// <summary>
        /// Original was GL_INTERLACE_SGIX = 0x8094
        /// </summary>
        InterlaceSgix = 32916,
        /// <summary>
        /// Original was GL_DETAIL_TEXTURE_2D_BINDING_SGIS = 0x8096
        /// </summary>
        DetailTexture2DBindingSgis = 32918,
        /// <summary>
        /// Original was GL_MULTISAMPLE = 0x809D
        /// </summary>
        Multisample = 32925,
        /// <summary>
        /// Original was GL_MULTISAMPLE_SGIS = 0x809D
        /// </summary>
        MultisampleSgis = 32925,
        /// <summary>
        /// Original was GL_SAMPLE_ALPHA_TO_COVERAGE = 0x809E
        /// </summary>
        SampleAlphaToCoverage = 32926,
        /// <summary>
        /// Original was GL_SAMPLE_ALPHA_TO_MASK_SGIS = 0x809E
        /// </summary>
        SampleAlphaToMaskSgis = 32926,
        /// <summary>
        /// Original was GL_SAMPLE_ALPHA_TO_ONE = 0x809F
        /// </summary>
        SampleAlphaToOne = 32927,
        /// <summary>
        /// Original was GL_SAMPLE_ALPHA_TO_ONE_SGIS = 0x809F
        /// </summary>
        SampleAlphaToOneSgis = 32927,
        /// <summary>
        /// Original was GL_SAMPLE_COVERAGE = 0x80A0
        /// </summary>
        SampleCoverage = 32928,
        /// <summary>
        /// Original was GL_SAMPLE_MASK_SGIS = 0x80A0
        /// </summary>
        SampleMaskSgis = 32928,
        /// <summary>
        /// Original was GL_SAMPLE_BUFFERS = 0x80A8
        /// </summary>
        SampleBuffers = 32936,
        /// <summary>
        /// Original was GL_SAMPLE_BUFFERS_SGIS = 0x80A8
        /// </summary>
        SampleBuffersSgis = 32936,
        /// <summary>
        /// Original was GL_SAMPLES = 0x80A9
        /// </summary>
        Samples = 32937,
        /// <summary>
        /// Original was GL_SAMPLES_SGIS = 0x80A9
        /// </summary>
        SamplesSgis = 32937,
        /// <summary>
        /// Original was GL_SAMPLE_COVERAGE_VALUE = 0x80AA
        /// </summary>
        SampleCoverageValue = 32938,
        /// <summary>
        /// Original was GL_SAMPLE_MASK_VALUE_SGIS = 0x80AA
        /// </summary>
        SampleMaskValueSgis = 32938,
        /// <summary>
        /// Original was GL_SAMPLE_COVERAGE_INVERT = 0x80AB
        /// </summary>
        SampleCoverageInvert = 32939,
        /// <summary>
        /// Original was GL_SAMPLE_MASK_INVERT_SGIS = 0x80AB
        /// </summary>
        SampleMaskInvertSgis = 32939,
        /// <summary>
        /// Original was GL_SAMPLE_PATTERN_SGIS = 0x80AC
        /// </summary>
        SamplePatternSgis = 32940,
        /// <summary>
        /// Original was GL_COLOR_MATRIX_SGI = 0x80B1
        /// </summary>
        ColorMatrixSgi = 32945,
        /// <summary>
        /// Original was GL_COLOR_MATRIX_STACK_DEPTH_SGI = 0x80B2
        /// </summary>
        ColorMatrixStackDepthSgi = 32946,
        /// <summary>
        /// Original was GL_MAX_COLOR_MATRIX_STACK_DEPTH_SGI = 0x80B3
        /// </summary>
        MaxColorMatrixStackDepthSgi = 32947,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_RED_SCALE_SGI = 0x80B4
        /// </summary>
        PostColorMatrixRedScaleSgi = 32948,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_GREEN_SCALE_SGI = 0x80B5
        /// </summary>
        PostColorMatrixGreenScaleSgi = 32949,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_BLUE_SCALE_SGI = 0x80B6
        /// </summary>
        PostColorMatrixBlueScaleSgi = 32950,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_ALPHA_SCALE_SGI = 0x80B7
        /// </summary>
        PostColorMatrixAlphaScaleSgi = 32951,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_RED_BIAS_SGI = 0x80B8
        /// </summary>
        PostColorMatrixRedBiasSgi = 32952,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_GREEN_BIAS_SGI = 0x80B9
        /// </summary>
        PostColorMatrixGreenBiasSgi = 32953,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_BLUE_BIAS_SGI = 0x80BA
        /// </summary>
        PostColorMatrixBlueBiasSgi = 32954,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_ALPHA_BIAS_SGI = 0x80BB
        /// </summary>
        PostColorMatrixAlphaBiasSgi = 32955,
        /// <summary>
        /// Original was GL_TEXTURE_COLOR_TABLE_SGI = 0x80BC
        /// </summary>
        TextureColorTableSgi = 32956,
        /// <summary>
        /// Original was GL_BLEND_DST_RGB = 0x80C8
        /// </summary>
        BlendDstRgb = 32968,
        /// <summary>
        /// Original was GL_BLEND_SRC_RGB = 0x80C9
        /// </summary>
        BlendSrcRgb = 32969,
        /// <summary>
        /// Original was GL_BLEND_DST_ALPHA = 0x80CA
        /// </summary>
        BlendDstAlpha = 32970,
        /// <summary>
        /// Original was GL_BLEND_SRC_ALPHA = 0x80CB
        /// </summary>
        BlendSrcAlpha = 32971,
        /// <summary>
        /// Original was GL_COLOR_TABLE_SGI = 0x80D0
        /// </summary>
        ColorTableSgi = 32976,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_COLOR_TABLE_SGI = 0x80D1
        /// </summary>
        PostConvolutionColorTableSgi = 32977,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_COLOR_TABLE_SGI = 0x80D2
        /// </summary>
        PostColorMatrixColorTableSgi = 32978,
        /// <summary>
        /// Original was GL_MAX_ELEMENTS_VERTICES = 0x80E8
        /// </summary>
        MaxElementsVertices = 33000,
        /// <summary>
        /// Original was GL_MAX_ELEMENTS_INDICES = 0x80E9
        /// </summary>
        MaxElementsIndices = 33001,
        /// <summary>
        /// Original was GL_POINT_SIZE_MIN = 0x8126
        /// </summary>
        PointSizeMin = 33062,
        /// <summary>
        /// Original was GL_POINT_SIZE_MIN_SGIS = 0x8126
        /// </summary>
        PointSizeMinSgis = 33062,
        /// <summary>
        /// Original was GL_POINT_SIZE_MAX = 0x8127
        /// </summary>
        PointSizeMax = 33063,
        /// <summary>
        /// Original was GL_POINT_SIZE_MAX_SGIS = 0x8127
        /// </summary>
        PointSizeMaxSgis = 33063,
        /// <summary>
        /// Original was GL_POINT_FADE_THRESHOLD_SIZE = 0x8128
        /// </summary>
        PointFadeThresholdSize = 33064,
        /// <summary>
        /// Original was GL_POINT_FADE_THRESHOLD_SIZE_SGIS = 0x8128
        /// </summary>
        PointFadeThresholdSizeSgis = 33064,
        /// <summary>
        /// Original was GL_DISTANCE_ATTENUATION_SGIS = 0x8129
        /// </summary>
        DistanceAttenuationSgis = 33065,
        /// <summary>
        /// Original was GL_POINT_DISTANCE_ATTENUATION = 0x8129
        /// </summary>
        PointDistanceAttenuation = 33065,
        /// <summary>
        /// Original was GL_FOG_FUNC_POINTS_SGIS = 0x812B
        /// </summary>
        FogFuncPointsSgis = 33067,
        /// <summary>
        /// Original was GL_MAX_FOG_FUNC_POINTS_SGIS = 0x812C
        /// </summary>
        MaxFogFuncPointsSgis = 33068,
        /// <summary>
        /// Original was GL_PACK_SKIP_VOLUMES_SGIS = 0x8130
        /// </summary>
        PackSkipVolumesSgis = 33072,
        /// <summary>
        /// Original was GL_PACK_IMAGE_DEPTH_SGIS = 0x8131
        /// </summary>
        PackImageDepthSgis = 33073,
        /// <summary>
        /// Original was GL_UNPACK_SKIP_VOLUMES_SGIS = 0x8132
        /// </summary>
        UnpackSkipVolumesSgis = 33074,
        /// <summary>
        /// Original was GL_UNPACK_IMAGE_DEPTH_SGIS = 0x8133
        /// </summary>
        UnpackImageDepthSgis = 33075,
        /// <summary>
        /// Original was GL_TEXTURE_4D_SGIS = 0x8134
        /// </summary>
        Texture4DSgis = 33076,
        /// <summary>
        /// Original was GL_MAX_4D_TEXTURE_SIZE_SGIS = 0x8138
        /// </summary>
        Max4DTextureSizeSgis = 33080,
        /// <summary>
        /// Original was GL_PIXEL_TEX_GEN_SGIX = 0x8139
        /// </summary>
        PixelTexGenSgix = 33081,
        /// <summary>
        /// Original was GL_PIXEL_TILE_BEST_ALIGNMENT_SGIX = 0x813E
        /// </summary>
        PixelTileBestAlignmentSgix = 33086,
        /// <summary>
        /// Original was GL_PIXEL_TILE_CACHE_INCREMENT_SGIX = 0x813F
        /// </summary>
        PixelTileCacheIncrementSgix = 33087,
        /// <summary>
        /// Original was GL_PIXEL_TILE_WIDTH_SGIX = 0x8140
        /// </summary>
        PixelTileWidthSgix = 33088,
        /// <summary>
        /// Original was GL_PIXEL_TILE_HEIGHT_SGIX = 0x8141
        /// </summary>
        PixelTileHeightSgix = 33089,
        /// <summary>
        /// Original was GL_PIXEL_TILE_GRID_WIDTH_SGIX = 0x8142
        /// </summary>
        PixelTileGridWidthSgix = 33090,
        /// <summary>
        /// Original was GL_PIXEL_TILE_GRID_HEIGHT_SGIX = 0x8143
        /// </summary>
        PixelTileGridHeightSgix = 33091,
        /// <summary>
        /// Original was GL_PIXEL_TILE_GRID_DEPTH_SGIX = 0x8144
        /// </summary>
        PixelTileGridDepthSgix = 33092,
        /// <summary>
        /// Original was GL_PIXEL_TILE_CACHE_SIZE_SGIX = 0x8145
        /// </summary>
        PixelTileCacheSizeSgix = 33093,
        /// <summary>
        /// Original was GL_SPRITE_SGIX = 0x8148
        /// </summary>
        SpriteSgix = 33096,
        /// <summary>
        /// Original was GL_SPRITE_MODE_SGIX = 0x8149
        /// </summary>
        SpriteModeSgix = 33097,
        /// <summary>
        /// Original was GL_SPRITE_AXIS_SGIX = 0x814A
        /// </summary>
        SpriteAxisSgix = 33098,
        /// <summary>
        /// Original was GL_SPRITE_TRANSLATION_SGIX = 0x814B
        /// </summary>
        SpriteTranslationSgix = 33099,
        /// <summary>
        /// Original was GL_TEXTURE_4D_BINDING_SGIS = 0x814F
        /// </summary>
        Texture4DBindingSgis = 33103,
        /// <summary>
        /// Original was GL_MAX_CLIPMAP_DEPTH_SGIX = 0x8177
        /// </summary>
        MaxClipmapDepthSgix = 33143,
        /// <summary>
        /// Original was GL_MAX_CLIPMAP_VIRTUAL_DEPTH_SGIX = 0x8178
        /// </summary>
        MaxClipmapVirtualDepthSgix = 33144,
        /// <summary>
        /// Original was GL_POST_TEXTURE_FILTER_BIAS_RANGE_SGIX = 0x817B
        /// </summary>
        PostTextureFilterBiasRangeSgix = 33147,
        /// <summary>
        /// Original was GL_POST_TEXTURE_FILTER_SCALE_RANGE_SGIX = 0x817C
        /// </summary>
        PostTextureFilterScaleRangeSgix = 33148,
        /// <summary>
        /// Original was GL_REFERENCE_PLANE_SGIX = 0x817D
        /// </summary>
        ReferencePlaneSgix = 33149,
        /// <summary>
        /// Original was GL_REFERENCE_PLANE_EQUATION_SGIX = 0x817E
        /// </summary>
        ReferencePlaneEquationSgix = 33150,
        /// <summary>
        /// Original was GL_IR_INSTRUMENT1_SGIX = 0x817F
        /// </summary>
        IrInstrument1Sgix = 33151,
        /// <summary>
        /// Original was GL_INSTRUMENT_MEASUREMENTS_SGIX = 0x8181
        /// </summary>
        InstrumentMeasurementsSgix = 33153,
        /// <summary>
        /// Original was GL_CALLIGRAPHIC_FRAGMENT_SGIX = 0x8183
        /// </summary>
        CalligraphicFragmentSgix = 33155,
        /// <summary>
        /// Original was GL_FRAMEZOOM_SGIX = 0x818B
        /// </summary>
        FramezoomSgix = 33163,
        /// <summary>
        /// Original was GL_FRAMEZOOM_FACTOR_SGIX = 0x818C
        /// </summary>
        FramezoomFactorSgix = 33164,
        /// <summary>
        /// Original was GL_MAX_FRAMEZOOM_FACTOR_SGIX = 0x818D
        /// </summary>
        MaxFramezoomFactorSgix = 33165,
        /// <summary>
        /// Original was GL_GENERATE_MIPMAP_HINT = 0x8192
        /// </summary>
        GenerateMipmapHint = 33170,
        /// <summary>
        /// Original was GL_GENERATE_MIPMAP_HINT_SGIS = 0x8192
        /// </summary>
        GenerateMipmapHintSgis = 33170,
        /// <summary>
        /// Original was GL_DEFORMATIONS_MASK_SGIX = 0x8196
        /// </summary>
        DeformationsMaskSgix = 33174,
        /// <summary>
        /// Original was GL_FOG_OFFSET_SGIX = 0x8198
        /// </summary>
        FogOffsetSgix = 33176,
        /// <summary>
        /// Original was GL_FOG_OFFSET_VALUE_SGIX = 0x8199
        /// </summary>
        FogOffsetValueSgix = 33177,
        /// <summary>
        /// Original was GL_LIGHT_MODEL_COLOR_CONTROL = 0x81F8
        /// </summary>
        LightModelColorControl = 33272,
        /// <summary>
        /// Original was GL_SHARED_TEXTURE_PALETTE_EXT = 0x81FB
        /// </summary>
        SharedTexturePaletteExt = 33275,
        /// <summary>
        /// Original was GL_MAJOR_VERSION = 0x821B
        /// </summary>
        MajorVersion = 33307,
        /// <summary>
        /// Original was GL_MINOR_VERSION = 0x821C
        /// </summary>
        MinorVersion = 33308,
        /// <summary>
        /// Original was GL_NUM_EXTENSIONS = 0x821D
        /// </summary>
        NumExtensions = 33309,
        /// <summary>
        /// Original was GL_CONTEXT_FLAGS = 0x821E
        /// </summary>
        ContextFlags = 33310,
        /// <summary>
        /// Original was GL_RESET_NOTIFICATION_STRATEGY = 0x8256
        /// </summary>
        ResetNotificationStrategy = 33366,
        /// <summary>
        /// Original was GL_PROGRAM_PIPELINE_BINDING = 0x825A
        /// </summary>
        ProgramPipelineBinding = 33370,
        /// <summary>
        /// Original was GL_MAX_VIEWPORTS = 0x825B
        /// </summary>
        MaxViewports = 33371,
        /// <summary>
        /// Original was GL_VIEWPORT_SUBPIXEL_BITS = 0x825C
        /// </summary>
        ViewportSubpixelBits = 33372,
        /// <summary>
        /// Original was GL_VIEWPORT_BOUNDS_RANGE = 0x825D
        /// </summary>
        ViewportBoundsRange = 33373,
        /// <summary>
        /// Original was GL_LAYER_PROVOKING_VERTEX = 0x825E
        /// </summary>
        LayerProvokingVertex = 33374,
        /// <summary>
        /// Original was GL_VIEWPORT_INDEX_PROVOKING_VERTEX = 0x825F
        /// </summary>
        ViewportIndexProvokingVertex = 33375,
        /// <summary>
        /// Original was GL_MAX_CULL_DISTANCES = 0x82F9
        /// </summary>
        MaxCullDistances = 33529,
        /// <summary>
        /// Original was GL_MAX_COMBINED_CLIP_AND_CULL_DISTANCES = 0x82FA
        /// </summary>
        MaxCombinedClipAndCullDistances = 33530,
        /// <summary>
        /// Original was GL_CONTEXT_RELEASE_BEHAVIOR = 0x82FB
        /// </summary>
        ContextReleaseBehavior = 33531,
        /// <summary>
        /// Original was GL_CONVOLUTION_HINT_SGIX = 0x8316
        /// </summary>
        ConvolutionHintSgix = 33558,
        /// <summary>
        /// Original was GL_ASYNC_MARKER_SGIX = 0x8329
        /// </summary>
        AsyncMarkerSgix = 33577,
        /// <summary>
        /// Original was GL_PIXEL_TEX_GEN_MODE_SGIX = 0x832B
        /// </summary>
        PixelTexGenModeSgix = 33579,
        /// <summary>
        /// Original was GL_ASYNC_HISTOGRAM_SGIX = 0x832C
        /// </summary>
        AsyncHistogramSgix = 33580,
        /// <summary>
        /// Original was GL_MAX_ASYNC_HISTOGRAM_SGIX = 0x832D
        /// </summary>
        MaxAsyncHistogramSgix = 33581,
        /// <summary>
        /// Original was GL_PIXEL_TEXTURE_SGIS = 0x8353
        /// </summary>
        PixelTextureSgis = 33619,
        /// <summary>
        /// Original was GL_ASYNC_TEX_IMAGE_SGIX = 0x835C
        /// </summary>
        AsyncTexImageSgix = 33628,
        /// <summary>
        /// Original was GL_ASYNC_DRAW_PIXELS_SGIX = 0x835D
        /// </summary>
        AsyncDrawPixelsSgix = 33629,
        /// <summary>
        /// Original was GL_ASYNC_READ_PIXELS_SGIX = 0x835E
        /// </summary>
        AsyncReadPixelsSgix = 33630,
        /// <summary>
        /// Original was GL_MAX_ASYNC_TEX_IMAGE_SGIX = 0x835F
        /// </summary>
        MaxAsyncTexImageSgix = 33631,
        /// <summary>
        /// Original was GL_MAX_ASYNC_DRAW_PIXELS_SGIX = 0x8360
        /// </summary>
        MaxAsyncDrawPixelsSgix = 33632,
        /// <summary>
        /// Original was GL_MAX_ASYNC_READ_PIXELS_SGIX = 0x8361
        /// </summary>
        MaxAsyncReadPixelsSgix = 33633,
        /// <summary>
        /// Original was GL_VERTEX_PRECLIP_SGIX = 0x83EE
        /// </summary>
        VertexPreclipSgix = 33774,
        /// <summary>
        /// Original was GL_VERTEX_PRECLIP_HINT_SGIX = 0x83EF
        /// </summary>
        VertexPreclipHintSgix = 33775,
        /// <summary>
        /// Original was GL_FRAGMENT_LIGHTING_SGIX = 0x8400
        /// </summary>
        FragmentLightingSgix = 33792,
        /// <summary>
        /// Original was GL_FRAGMENT_COLOR_MATERIAL_SGIX = 0x8401
        /// </summary>
        FragmentColorMaterialSgix = 33793,
        /// <summary>
        /// Original was GL_FRAGMENT_COLOR_MATERIAL_FACE_SGIX = 0x8402
        /// </summary>
        FragmentColorMaterialFaceSgix = 33794,
        /// <summary>
        /// Original was GL_FRAGMENT_COLOR_MATERIAL_PARAMETER_SGIX = 0x8403
        /// </summary>
        FragmentColorMaterialParameterSgix = 33795,
        /// <summary>
        /// Original was GL_MAX_FRAGMENT_LIGHTS_SGIX = 0x8404
        /// </summary>
        MaxFragmentLightsSgix = 33796,
        /// <summary>
        /// Original was GL_MAX_ACTIVE_LIGHTS_SGIX = 0x8405
        /// </summary>
        MaxActiveLightsSgix = 33797,
        /// <summary>
        /// Original was GL_LIGHT_ENV_MODE_SGIX = 0x8407
        /// </summary>
        LightEnvModeSgix = 33799,
        /// <summary>
        /// Original was GL_FRAGMENT_LIGHT_MODEL_LOCAL_VIEWER_SGIX = 0x8408
        /// </summary>
        FragmentLightModelLocalViewerSgix = 33800,
        /// <summary>
        /// Original was GL_FRAGMENT_LIGHT_MODEL_TWO_SIDE_SGIX = 0x8409
        /// </summary>
        FragmentLightModelTwoSideSgix = 33801,
        /// <summary>
        /// Original was GL_FRAGMENT_LIGHT_MODEL_AMBIENT_SGIX = 0x840A
        /// </summary>
        FragmentLightModelAmbientSgix = 33802,
        /// <summary>
        /// Original was GL_FRAGMENT_LIGHT_MODEL_NORMAL_INTERPOLATION_SGIX = 0x840B
        /// </summary>
        FragmentLightModelNormalInterpolationSgix = 33803,
        /// <summary>
        /// Original was GL_FRAGMENT_LIGHT0_SGIX = 0x840C
        /// </summary>
        FragmentLight0Sgix = 33804,
        /// <summary>
        /// Original was GL_PACK_RESAMPLE_SGIX = 0x842E
        /// </summary>
        PackResampleSgix = 33838,
        /// <summary>
        /// Original was GL_UNPACK_RESAMPLE_SGIX = 0x842F
        /// </summary>
        UnpackResampleSgix = 33839,
        /// <summary>
        /// Original was GL_CURRENT_FOG_COORD = 0x8453
        /// </summary>
        CurrentFogCoord = 33875,
        /// <summary>
        /// Original was GL_FOG_COORD_ARRAY_TYPE = 0x8454
        /// </summary>
        FogCoordArrayType = 33876,
        /// <summary>
        /// Original was GL_FOG_COORD_ARRAY_STRIDE = 0x8455
        /// </summary>
        FogCoordArrayStride = 33877,
        /// <summary>
        /// Original was GL_COLOR_SUM = 0x8458
        /// </summary>
        ColorSum = 33880,
        /// <summary>
        /// Original was GL_CURRENT_SECONDARY_COLOR = 0x8459
        /// </summary>
        CurrentSecondaryColor = 33881,
        /// <summary>
        /// Original was GL_SECONDARY_COLOR_ARRAY_SIZE = 0x845A
        /// </summary>
        SecondaryColorArraySize = 33882,
        /// <summary>
        /// Original was GL_SECONDARY_COLOR_ARRAY_TYPE = 0x845B
        /// </summary>
        SecondaryColorArrayType = 33883,
        /// <summary>
        /// Original was GL_SECONDARY_COLOR_ARRAY_STRIDE = 0x845C
        /// </summary>
        SecondaryColorArrayStride = 33884,
        /// <summary>
        /// Original was GL_CURRENT_RASTER_SECONDARY_COLOR = 0x845F
        /// </summary>
        CurrentRasterSecondaryColor = 33887,
        /// <summary>
        /// Original was GL_ALIASED_POINT_SIZE_RANGE = 0x846D
        /// </summary>
        AliasedPointSizeRange = 33901,
        /// <summary>
        /// Original was GL_ALIASED_LINE_WIDTH_RANGE = 0x846E
        /// </summary>
        AliasedLineWidthRange = 33902,
        /// <summary>
        /// Original was GL_ACTIVE_TEXTURE = 0x84E0
        /// </summary>
        ActiveTexture = 34016,
        /// <summary>
        /// Original was GL_CLIENT_ACTIVE_TEXTURE = 0x84E1
        /// </summary>
        ClientActiveTexture = 34017,
        /// <summary>
        /// Original was GL_MAX_TEXTURE_UNITS = 0x84E2
        /// </summary>
        MaxTextureUnits = 34018,
        /// <summary>
        /// Original was GL_TRANSPOSE_MODELVIEW_MATRIX = 0x84E3
        /// </summary>
        TransposeModelviewMatrix = 34019,
        /// <summary>
        /// Original was GL_TRANSPOSE_PROJECTION_MATRIX = 0x84E4
        /// </summary>
        TransposeProjectionMatrix = 34020,
        /// <summary>
        /// Original was GL_TRANSPOSE_TEXTURE_MATRIX = 0x84E5
        /// </summary>
        TransposeTextureMatrix = 34021,
        /// <summary>
        /// Original was GL_TRANSPOSE_COLOR_MATRIX = 0x84E6
        /// </summary>
        TransposeColorMatrix = 34022,
        /// <summary>
        /// Original was GL_MAX_RENDERBUFFER_SIZE = 0x84E8
        /// </summary>
        MaxRenderbufferSize = 34024,
        /// <summary>
        /// Original was GL_MAX_RENDERBUFFER_SIZE_EXT = 0x84E8
        /// </summary>
        MaxRenderbufferSizeExt = 34024,
        /// <summary>
        /// Original was GL_TEXTURE_COMPRESSION_HINT = 0x84EF
        /// </summary>
        TextureCompressionHint = 34031,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_RECTANGLE = 0x84F6
        /// </summary>
        TextureBindingRectangle = 34038,
        /// <summary>
        /// Original was GL_MAX_RECTANGLE_TEXTURE_SIZE = 0x84F8
        /// </summary>
        MaxRectangleTextureSize = 34040,
        /// <summary>
        /// Original was GL_MAX_TEXTURE_LOD_BIAS = 0x84FD
        /// </summary>
        MaxTextureLodBias = 34045,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP = 0x8513
        /// </summary>
        TextureCubeMap = 34067,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_CUBE_MAP = 0x8514
        /// </summary>
        TextureBindingCubeMap = 34068,
        /// <summary>
        /// Original was GL_MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C
        /// </summary>
        MaxCubeMapTextureSize = 34076,
        /// <summary>
        /// Original was GL_PACK_SUBSAMPLE_RATE_SGIX = 0x85A0
        /// </summary>
        PackSubsampleRateSgix = 34208,
        /// <summary>
        /// Original was GL_UNPACK_SUBSAMPLE_RATE_SGIX = 0x85A1
        /// </summary>
        UnpackSubsampleRateSgix = 34209,
        /// <summary>
        /// Original was GL_VERTEX_ARRAY_BINDING = 0x85B5
        /// </summary>
        VertexArrayBinding = 34229,
        /// <summary>
        /// Original was GL_PROGRAM_POINT_SIZE = 0x8642
        /// </summary>
        ProgramPointSize = 34370,
        /// <summary>
        /// Original was GL_DEPTH_CLAMP = 0x864F
        /// </summary>
        DepthClamp = 34383,
        /// <summary>
        /// Original was GL_NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2
        /// </summary>
        NumCompressedTextureFormats = 34466,
        /// <summary>
        /// Original was GL_COMPRESSED_TEXTURE_FORMATS = 0x86A3
        /// </summary>
        CompressedTextureFormats = 34467,
        /// <summary>
        /// Original was GL_NUM_PROGRAM_BINARY_FORMATS = 0x87FE
        /// </summary>
        NumProgramBinaryFormats = 34814,
        /// <summary>
        /// Original was GL_PROGRAM_BINARY_FORMATS = 0x87FF
        /// </summary>
        ProgramBinaryFormats = 34815,
        /// <summary>
        /// Original was GL_STENCIL_BACK_FUNC = 0x8800
        /// </summary>
        StencilBackFunc = 34816,
        /// <summary>
        /// Original was GL_STENCIL_BACK_FAIL = 0x8801
        /// </summary>
        StencilBackFail = 34817,
        /// <summary>
        /// Original was GL_STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802
        /// </summary>
        StencilBackPassDepthFail = 34818,
        /// <summary>
        /// Original was GL_STENCIL_BACK_PASS_DEPTH_PASS = 0x8803
        /// </summary>
        StencilBackPassDepthPass = 34819,
        /// <summary>
        /// Original was GL_RGBA_FLOAT_MODE = 0x8820
        /// </summary>
        RgbaFloatMode = 34848,
        /// <summary>
        /// Original was GL_MAX_DRAW_BUFFERS = 0x8824
        /// </summary>
        MaxDrawBuffers = 34852,
        /// <summary>
        /// Original was GL_DRAW_BUFFER0 = 0x8825
        /// </summary>
        DrawBuffer0 = 34853,
        /// <summary>
        /// Original was GL_DRAW_BUFFER1 = 0x8826
        /// </summary>
        DrawBuffer1 = 34854,
        /// <summary>
        /// Original was GL_DRAW_BUFFER2 = 0x8827
        /// </summary>
        DrawBuffer2 = 34855,
        /// <summary>
        /// Original was GL_DRAW_BUFFER3 = 0x8828
        /// </summary>
        DrawBuffer3 = 34856,
        /// <summary>
        /// Original was GL_DRAW_BUFFER4 = 0x8829
        /// </summary>
        DrawBuffer4 = 34857,
        /// <summary>
        /// Original was GL_DRAW_BUFFER5 = 0x882A
        /// </summary>
        DrawBuffer5 = 34858,
        /// <summary>
        /// Original was GL_DRAW_BUFFER6 = 0x882B
        /// </summary>
        DrawBuffer6 = 34859,
        /// <summary>
        /// Original was GL_DRAW_BUFFER7 = 0x882C
        /// </summary>
        DrawBuffer7 = 34860,
        /// <summary>
        /// Original was GL_DRAW_BUFFER8 = 0x882D
        /// </summary>
        DrawBuffer8 = 34861,
        /// <summary>
        /// Original was GL_DRAW_BUFFER9 = 0x882E
        /// </summary>
        DrawBuffer9 = 34862,
        /// <summary>
        /// Original was GL_DRAW_BUFFER10 = 0x882F
        /// </summary>
        DrawBuffer10 = 34863,
        /// <summary>
        /// Original was GL_DRAW_BUFFER11 = 0x8830
        /// </summary>
        DrawBuffer11 = 34864,
        /// <summary>
        /// Original was GL_DRAW_BUFFER12 = 0x8831
        /// </summary>
        DrawBuffer12 = 34865,
        /// <summary>
        /// Original was GL_DRAW_BUFFER13 = 0x8832
        /// </summary>
        DrawBuffer13 = 34866,
        /// <summary>
        /// Original was GL_DRAW_BUFFER14 = 0x8833
        /// </summary>
        DrawBuffer14 = 34867,
        /// <summary>
        /// Original was GL_DRAW_BUFFER15 = 0x8834
        /// </summary>
        DrawBuffer15 = 34868,
        /// <summary>
        /// Original was GL_BLEND_EQUATION_ALPHA = 0x883D
        /// </summary>
        BlendEquationAlpha = 34877,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_SEAMLESS = 0x884F
        /// </summary>
        TextureCubeMapSeamless = 34895,
        /// <summary>
        /// Original was GL_POINT_SPRITE = 0x8861
        /// </summary>
        PointSprite = 34913,
        /// <summary>
        /// Original was GL_MAX_VERTEX_ATTRIBS = 0x8869
        /// </summary>
        MaxVertexAttribs = 34921,
        /// <summary>
        /// Original was GL_MAX_TESS_CONTROL_INPUT_COMPONENTS = 0x886C
        /// </summary>
        MaxTessControlInputComponents = 34924,
        /// <summary>
        /// Original was GL_MAX_TESS_EVALUATION_INPUT_COMPONENTS = 0x886D
        /// </summary>
        MaxTessEvaluationInputComponents = 34925,
        /// <summary>
        /// Original was GL_MAX_TEXTURE_COORDS = 0x8871
        /// </summary>
        MaxTextureCoords = 34929,
        /// <summary>
        /// Original was GL_MAX_TEXTURE_IMAGE_UNITS = 0x8872
        /// </summary>
        MaxTextureImageUnits = 34930,
        /// <summary>
        /// Original was GL_ARRAY_BUFFER_BINDING = 0x8894
        /// </summary>
        ArrayBufferBinding = 34964,
        /// <summary>
        /// Original was GL_ELEMENT_ARRAY_BUFFER_BINDING = 0x8895
        /// </summary>
        ElementArrayBufferBinding = 34965,
        /// <summary>
        /// Original was GL_VERTEX_ARRAY_BUFFER_BINDING = 0x8896
        /// </summary>
        VertexArrayBufferBinding = 34966,
        /// <summary>
        /// Original was GL_NORMAL_ARRAY_BUFFER_BINDING = 0x8897
        /// </summary>
        NormalArrayBufferBinding = 34967,
        /// <summary>
        /// Original was GL_COLOR_ARRAY_BUFFER_BINDING = 0x8898
        /// </summary>
        ColorArrayBufferBinding = 34968,
        /// <summary>
        /// Original was GL_INDEX_ARRAY_BUFFER_BINDING = 0x8899
        /// </summary>
        IndexArrayBufferBinding = 34969,
        /// <summary>
        /// Original was GL_TEXTURE_COORD_ARRAY_BUFFER_BINDING = 0x889A
        /// </summary>
        TextureCoordArrayBufferBinding = 34970,
        /// <summary>
        /// Original was GL_EDGE_FLAG_ARRAY_BUFFER_BINDING = 0x889B
        /// </summary>
        EdgeFlagArrayBufferBinding = 34971,
        /// <summary>
        /// Original was GL_SECONDARY_COLOR_ARRAY_BUFFER_BINDING = 0x889C
        /// </summary>
        SecondaryColorArrayBufferBinding = 34972,
        /// <summary>
        /// Original was GL_FOG_COORD_ARRAY_BUFFER_BINDING = 0x889D
        /// </summary>
        FogCoordArrayBufferBinding = 34973,
        /// <summary>
        /// Original was GL_WEIGHT_ARRAY_BUFFER_BINDING = 0x889E
        /// </summary>
        WeightArrayBufferBinding = 34974,
        /// <summary>
        /// Original was GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F
        /// </summary>
        VertexAttribArrayBufferBinding = 34975,
        /// <summary>
        /// Original was GL_PIXEL_PACK_BUFFER_BINDING = 0x88ED
        /// </summary>
        PixelPackBufferBinding = 35053,
        /// <summary>
        /// Original was GL_PIXEL_UNPACK_BUFFER_BINDING = 0x88EF
        /// </summary>
        PixelUnpackBufferBinding = 35055,
        /// <summary>
        /// Original was GL_MAX_DUAL_SOURCE_DRAW_BUFFERS = 0x88FC
        /// </summary>
        MaxDualSourceDrawBuffers = 35068,
        /// <summary>
        /// Original was GL_MAX_ARRAY_TEXTURE_LAYERS = 0x88FF
        /// </summary>
        MaxArrayTextureLayers = 35071,
        /// <summary>
        /// Original was GL_MIN_PROGRAM_TEXEL_OFFSET = 0x8904
        /// </summary>
        MinProgramTexelOffset = 35076,
        /// <summary>
        /// Original was GL_MAX_PROGRAM_TEXEL_OFFSET = 0x8905
        /// </summary>
        MaxProgramTexelOffset = 35077,
        /// <summary>
        /// Original was GL_SAMPLER_BINDING = 0x8919
        /// </summary>
        SamplerBinding = 35097,
        /// <summary>
        /// Original was GL_CLAMP_VERTEX_COLOR = 0x891A
        /// </summary>
        ClampVertexColor = 35098,
        /// <summary>
        /// Original was GL_CLAMP_FRAGMENT_COLOR = 0x891B
        /// </summary>
        ClampFragmentColor = 35099,
        /// <summary>
        /// Original was GL_CLAMP_READ_COLOR = 0x891C
        /// </summary>
        ClampReadColor = 35100,
        /// <summary>
        /// Original was GL_MAX_VERTEX_UNIFORM_BLOCKS = 0x8A2B
        /// </summary>
        MaxVertexUniformBlocks = 35371,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_UNIFORM_BLOCKS = 0x8A2C
        /// </summary>
        MaxGeometryUniformBlocks = 35372,
        /// <summary>
        /// Original was GL_MAX_FRAGMENT_UNIFORM_BLOCKS = 0x8A2D
        /// </summary>
        MaxFragmentUniformBlocks = 35373,
        /// <summary>
        /// Original was GL_MAX_COMBINED_UNIFORM_BLOCKS = 0x8A2E
        /// </summary>
        MaxCombinedUniformBlocks = 35374,
        /// <summary>
        /// Original was GL_MAX_UNIFORM_BUFFER_BINDINGS = 0x8A2F
        /// </summary>
        MaxUniformBufferBindings = 35375,
        /// <summary>
        /// Original was GL_MAX_UNIFORM_BLOCK_SIZE = 0x8A30
        /// </summary>
        MaxUniformBlockSize = 35376,
        /// <summary>
        /// Original was GL_MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS = 0x8A31
        /// </summary>
        MaxCombinedVertexUniformComponents = 35377,
        /// <summary>
        /// Original was GL_MAX_COMBINED_GEOMETRY_UNIFORM_COMPONENTS = 0x8A32
        /// </summary>
        MaxCombinedGeometryUniformComponents = 35378,
        /// <summary>
        /// Original was GL_MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS = 0x8A33
        /// </summary>
        MaxCombinedFragmentUniformComponents = 35379,
        /// <summary>
        /// Original was GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8A34
        /// </summary>
        UniformBufferOffsetAlignment = 35380,
        /// <summary>
        /// Original was GL_MAX_FRAGMENT_UNIFORM_COMPONENTS = 0x8B49
        /// </summary>
        MaxFragmentUniformComponents = 35657,
        /// <summary>
        /// Original was GL_MAX_VERTEX_UNIFORM_COMPONENTS = 0x8B4A
        /// </summary>
        MaxVertexUniformComponents = 35658,
        /// <summary>
        /// Original was GL_MAX_VARYING_COMPONENTS = 0x8B4B
        /// </summary>
        MaxVaryingComponents = 35659,
        /// <summary>
        /// Original was GL_MAX_VARYING_FLOATS = 0x8B4B
        /// </summary>
        MaxVaryingFloats = 35659,
        /// <summary>
        /// Original was GL_MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C
        /// </summary>
        MaxVertexTextureImageUnits = 35660,
        /// <summary>
        /// Original was GL_MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D
        /// </summary>
        MaxCombinedTextureImageUnits = 35661,
        /// <summary>
        /// Original was GL_FRAGMENT_SHADER_DERIVATIVE_HINT = 0x8B8B
        /// </summary>
        FragmentShaderDerivativeHint = 35723,
        /// <summary>
        /// Original was GL_CURRENT_PROGRAM = 0x8B8D
        /// </summary>
        CurrentProgram = 35725,
        /// <summary>
        /// Original was GL_IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A
        /// </summary>
        ImplementationColorReadType = 35738,
        /// <summary>
        /// Original was GL_IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B
        /// </summary>
        ImplementationColorReadFormat = 35739,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_1D_ARRAY = 0x8C1C
        /// </summary>
        TextureBinding1DArray = 35868,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_2D_ARRAY = 0x8C1D
        /// </summary>
        TextureBinding2DArray = 35869,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_TEXTURE_IMAGE_UNITS = 0x8C29
        /// </summary>
        MaxGeometryTextureImageUnits = 35881,
        /// <summary>
        /// Original was GL_TEXTURE_BUFFER = 0x8C2A
        /// </summary>
        TextureBuffer = 35882,
        /// <summary>
        /// Original was GL_MAX_TEXTURE_BUFFER_SIZE = 0x8C2B
        /// </summary>
        MaxTextureBufferSize = 35883,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_BUFFER = 0x8C2C
        /// </summary>
        TextureBindingBuffer = 35884,
        /// <summary>
        /// Original was GL_TEXTURE_BUFFER_DATA_STORE_BINDING = 0x8C2D
        /// </summary>
        TextureBufferDataStoreBinding = 35885,
        /// <summary>
        /// Original was GL_SAMPLE_SHADING = 0x8C36
        /// </summary>
        SampleShading = 35894,
        /// <summary>
        /// Original was GL_MIN_SAMPLE_SHADING_VALUE = 0x8C37
        /// </summary>
        MinSampleShadingValue = 35895,
        /// <summary>
        /// Original was GL_MAX_TRANSFORM_FEEDBACK_SEPARATE_COMPONENTS = 0x8C80
        /// </summary>
        MaxTransformFeedbackSeparateComponents = 35968,
        /// <summary>
        /// Original was GL_MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS = 0x8C8A
        /// </summary>
        MaxTransformFeedbackInterleavedComponents = 35978,
        /// <summary>
        /// Original was GL_MAX_TRANSFORM_FEEDBACK_SEPARATE_ATTRIBS = 0x8C8B
        /// </summary>
        MaxTransformFeedbackSeparateAttribs = 35979,
        /// <summary>
        /// Original was GL_STENCIL_BACK_REF = 0x8CA3
        /// </summary>
        StencilBackRef = 36003,
        /// <summary>
        /// Original was GL_STENCIL_BACK_VALUE_MASK = 0x8CA4
        /// </summary>
        StencilBackValueMask = 36004,
        /// <summary>
        /// Original was GL_STENCIL_BACK_WRITEMASK = 0x8CA5
        /// </summary>
        StencilBackWritemask = 36005,
        /// <summary>
        /// Original was GL_DRAW_FRAMEBUFFER_BINDING = 0x8CA6
        /// </summary>
        DrawFramebufferBinding = 36006,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_BINDING = 0x8CA6
        /// </summary>
        FramebufferBinding = 36006,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_BINDING_EXT = 0x8CA6
        /// </summary>
        FramebufferBindingExt = 36006,
        /// <summary>
        /// Original was GL_RENDERBUFFER_BINDING = 0x8CA7
        /// </summary>
        RenderbufferBinding = 36007,
        /// <summary>
        /// Original was GL_RENDERBUFFER_BINDING_EXT = 0x8CA7
        /// </summary>
        RenderbufferBindingExt = 36007,
        /// <summary>
        /// Original was GL_READ_FRAMEBUFFER_BINDING = 0x8CAA
        /// </summary>
        ReadFramebufferBinding = 36010,
        /// <summary>
        /// Original was GL_MAX_COLOR_ATTACHMENTS = 0x8CDF
        /// </summary>
        MaxColorAttachments = 36063,
        /// <summary>
        /// Original was GL_MAX_COLOR_ATTACHMENTS_EXT = 0x8CDF
        /// </summary>
        MaxColorAttachmentsExt = 36063,
        /// <summary>
        /// Original was GL_MAX_SAMPLES = 0x8D57
        /// </summary>
        MaxSamples = 36183,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_SRGB = 0x8DB9
        /// </summary>
        FramebufferSrgb = 36281,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_VARYING_COMPONENTS = 0x8DDD
        /// </summary>
        MaxGeometryVaryingComponents = 36317,
        /// <summary>
        /// Original was GL_MAX_VERTEX_VARYING_COMPONENTS = 0x8DDE
        /// </summary>
        MaxVertexVaryingComponents = 36318,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_UNIFORM_COMPONENTS = 0x8DDF
        /// </summary>
        MaxGeometryUniformComponents = 36319,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_OUTPUT_VERTICES = 0x8DE0
        /// </summary>
        MaxGeometryOutputVertices = 36320,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_TOTAL_OUTPUT_COMPONENTS = 0x8DE1
        /// </summary>
        MaxGeometryTotalOutputComponents = 36321,
        /// <summary>
        /// Original was GL_MAX_SUBROUTINES = 0x8DE7
        /// </summary>
        MaxSubroutines = 36327,
        /// <summary>
        /// Original was GL_MAX_SUBROUTINE_UNIFORM_LOCATIONS = 0x8DE8
        /// </summary>
        MaxSubroutineUniformLocations = 36328,
        /// <summary>
        /// Original was GL_SHADER_BINARY_FORMATS = 0x8DF8
        /// </summary>
        ShaderBinaryFormats = 36344,
        /// <summary>
        /// Original was GL_NUM_SHADER_BINARY_FORMATS = 0x8DF9
        /// </summary>
        NumShaderBinaryFormats = 36345,
        /// <summary>
        /// Original was GL_SHADER_COMPILER = 0x8DFA
        /// </summary>
        ShaderCompiler = 36346,
        /// <summary>
        /// Original was GL_MAX_VERTEX_UNIFORM_VECTORS = 0x8DFB
        /// </summary>
        MaxVertexUniformVectors = 36347,
        /// <summary>
        /// Original was GL_MAX_VARYING_VECTORS = 0x8DFC
        /// </summary>
        MaxVaryingVectors = 36348,
        /// <summary>
        /// Original was GL_MAX_FRAGMENT_UNIFORM_VECTORS = 0x8DFD
        /// </summary>
        MaxFragmentUniformVectors = 36349,
        /// <summary>
        /// Original was GL_MAX_COMBINED_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E1E
        /// </summary>
        MaxCombinedTessControlUniformComponents = 36382,
        /// <summary>
        /// Original was GL_MAX_COMBINED_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E1F
        /// </summary>
        MaxCombinedTessEvaluationUniformComponents = 36383,
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_BUFFER_PAUSED = 0x8E23
        /// </summary>
        TransformFeedbackBufferPaused = 36387,
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_BUFFER_ACTIVE = 0x8E24
        /// </summary>
        TransformFeedbackBufferActive = 36388,
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_BINDING = 0x8E25
        /// </summary>
        TransformFeedbackBinding = 36389,
        /// <summary>
        /// Original was GL_TIMESTAMP = 0x8E28
        /// </summary>
        Timestamp = 36392,
        /// <summary>
        /// Original was GL_QUADS_FOLLOW_PROVOKING_VERTEX_CONVENTION = 0x8E4C
        /// </summary>
        QuadsFollowProvokingVertexConvention = 36428,
        /// <summary>
        /// Original was GL_PROVOKING_VERTEX = 0x8E4F
        /// </summary>
        ProvokingVertex = 36431,
        /// <summary>
        /// Original was GL_SAMPLE_MASK = 0x8E51
        /// </summary>
        SampleMask = 36433,
        /// <summary>
        /// Original was GL_MAX_SAMPLE_MASK_WORDS = 0x8E59
        /// </summary>
        MaxSampleMaskWords = 36441,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_SHADER_INVOCATIONS = 0x8E5A
        /// </summary>
        MaxGeometryShaderInvocations = 36442,
        /// <summary>
        /// Original was GL_MIN_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5B
        /// </summary>
        MinFragmentInterpolationOffset = 36443,
        /// <summary>
        /// Original was GL_MAX_FRAGMENT_INTERPOLATION_OFFSET = 0x8E5C
        /// </summary>
        MaxFragmentInterpolationOffset = 36444,
        /// <summary>
        /// Original was GL_FRAGMENT_INTERPOLATION_OFFSET_BITS = 0x8E5D
        /// </summary>
        FragmentInterpolationOffsetBits = 36445,
        /// <summary>
        /// Original was GL_MIN_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5E
        /// </summary>
        MinProgramTextureGatherOffset = 36446,
        /// <summary>
        /// Original was GL_MAX_PROGRAM_TEXTURE_GATHER_OFFSET = 0x8E5F
        /// </summary>
        MaxProgramTextureGatherOffset = 36447,
        /// <summary>
        /// Original was GL_MAX_TRANSFORM_FEEDBACK_BUFFERS = 0x8E70
        /// </summary>
        MaxTransformFeedbackBuffers = 36464,
        /// <summary>
        /// Original was GL_MAX_VERTEX_STREAMS = 0x8E71
        /// </summary>
        MaxVertexStreams = 36465,
        /// <summary>
        /// Original was GL_PATCH_VERTICES = 0x8E72
        /// </summary>
        PatchVertices = 36466,
        /// <summary>
        /// Original was GL_PATCH_DEFAULT_INNER_LEVEL = 0x8E73
        /// </summary>
        PatchDefaultInnerLevel = 36467,
        /// <summary>
        /// Original was GL_PATCH_DEFAULT_OUTER_LEVEL = 0x8E74
        /// </summary>
        PatchDefaultOuterLevel = 36468,
        /// <summary>
        /// Original was GL_MAX_PATCH_VERTICES = 0x8E7D
        /// </summary>
        MaxPatchVertices = 36477,
        /// <summary>
        /// Original was GL_MAX_TESS_GEN_LEVEL = 0x8E7E
        /// </summary>
        MaxTessGenLevel = 36478,
        /// <summary>
        /// Original was GL_MAX_TESS_CONTROL_UNIFORM_COMPONENTS = 0x8E7F
        /// </summary>
        MaxTessControlUniformComponents = 36479,
        /// <summary>
        /// Original was GL_MAX_TESS_EVALUATION_UNIFORM_COMPONENTS = 0x8E80
        /// </summary>
        MaxTessEvaluationUniformComponents = 36480,
        /// <summary>
        /// Original was GL_MAX_TESS_CONTROL_TEXTURE_IMAGE_UNITS = 0x8E81
        /// </summary>
        MaxTessControlTextureImageUnits = 36481,
        /// <summary>
        /// Original was GL_MAX_TESS_EVALUATION_TEXTURE_IMAGE_UNITS = 0x8E82
        /// </summary>
        MaxTessEvaluationTextureImageUnits = 36482,
        /// <summary>
        /// Original was GL_MAX_TESS_CONTROL_OUTPUT_COMPONENTS = 0x8E83
        /// </summary>
        MaxTessControlOutputComponents = 36483,
        /// <summary>
        /// Original was GL_MAX_TESS_PATCH_COMPONENTS = 0x8E84
        /// </summary>
        MaxTessPatchComponents = 36484,
        /// <summary>
        /// Original was GL_MAX_TESS_CONTROL_TOTAL_OUTPUT_COMPONENTS = 0x8E85
        /// </summary>
        MaxTessControlTotalOutputComponents = 36485,
        /// <summary>
        /// Original was GL_MAX_TESS_EVALUATION_OUTPUT_COMPONENTS = 0x8E86
        /// </summary>
        MaxTessEvaluationOutputComponents = 36486,
        /// <summary>
        /// Original was GL_MAX_TESS_CONTROL_UNIFORM_BLOCKS = 0x8E89
        /// </summary>
        MaxTessControlUniformBlocks = 36489,
        /// <summary>
        /// Original was GL_MAX_TESS_EVALUATION_UNIFORM_BLOCKS = 0x8E8A
        /// </summary>
        MaxTessEvaluationUniformBlocks = 36490,
        /// <summary>
        /// Original was GL_DRAW_INDIRECT_BUFFER_BINDING = 0x8F43
        /// </summary>
        DrawIndirectBufferBinding = 36675,
        /// <summary>
        /// Original was GL_MAX_VERTEX_IMAGE_UNIFORMS = 0x90CA
        /// </summary>
        MaxVertexImageUniforms = 37066,
        /// <summary>
        /// Original was GL_MAX_TESS_CONTROL_IMAGE_UNIFORMS = 0x90CB
        /// </summary>
        MaxTessControlImageUniforms = 37067,
        /// <summary>
        /// Original was GL_MAX_TESS_EVALUATION_IMAGE_UNIFORMS = 0x90CC
        /// </summary>
        MaxTessEvaluationImageUniforms = 37068,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_IMAGE_UNIFORMS = 0x90CD
        /// </summary>
        MaxGeometryImageUniforms = 37069,
        /// <summary>
        /// Original was GL_MAX_FRAGMENT_IMAGE_UNIFORMS = 0x90CE
        /// </summary>
        MaxFragmentImageUniforms = 37070,
        /// <summary>
        /// Original was GL_MAX_COMBINED_IMAGE_UNIFORMS = 0x90CF
        /// </summary>
        MaxCombinedImageUniforms = 37071,
        /// <summary>
        /// Original was GL_CONTEXT_ROBUST_ACCESS = 0x90F3
        /// </summary>
        ContextRobustAccess = 37107,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_2D_MULTISAMPLE = 0x9104
        /// </summary>
        TextureBinding2DMultisample = 37124,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY = 0x9105
        /// </summary>
        TextureBinding2DMultisampleArray = 37125,
        /// <summary>
        /// Original was GL_MAX_COLOR_TEXTURE_SAMPLES = 0x910E
        /// </summary>
        MaxColorTextureSamples = 37134,
        /// <summary>
        /// Original was GL_MAX_DEPTH_TEXTURE_SAMPLES = 0x910F
        /// </summary>
        MaxDepthTextureSamples = 37135,
        /// <summary>
        /// Original was GL_MAX_INTEGER_SAMPLES = 0x9110
        /// </summary>
        MaxIntegerSamples = 37136,
        /// <summary>
        /// Original was GL_MAX_VERTEX_OUTPUT_COMPONENTS = 0x9122
        /// </summary>
        MaxVertexOutputComponents = 37154,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_INPUT_COMPONENTS = 0x9123
        /// </summary>
        MaxGeometryInputComponents = 37155,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_OUTPUT_COMPONENTS = 0x9124
        /// </summary>
        MaxGeometryOutputComponents = 37156,
        /// <summary>
        /// Original was GL_MAX_FRAGMENT_INPUT_COMPONENTS = 0x9125
        /// </summary>
        MaxFragmentInputComponents = 37157,
        /// <summary>
        /// Original was GL_MAX_COMPUTE_IMAGE_UNIFORMS = 0x91BD
        /// </summary>
        MaxComputeImageUniforms = 37309,
        /// <summary>
        /// Original was GL_CLIP_ORIGIN = 0x935C
        /// </summary>
        ClipOrigin = 37724,
        /// <summary>
        /// Original was GL_CLIP_DEPTH_MODE = 0x935D
        /// </summary>
        ClipDepthMode = 37725,
        /// <summary>
        /// Original was GL_DEVICE_UUID_EXT = 0x9597
        /// </summary>
        DeviceUuidExt = 38295,
        /// <summary>
        /// Original was GL_DRIVER_UUID_EXT = 0x9598
        /// </summary>
        DriverUuidExt = 38296,
        /// <summary>
        /// Original was GL_DEVICE_LUID_EXT = 0x9599
        /// </summary>
        DeviceLuidExt = 38297,
        /// <summary>
        /// Original was GL_DEVICE_NODE_MASK_EXT = 0x959A
        /// </summary>
        DeviceNodeMaskExt = 38298
    }
}