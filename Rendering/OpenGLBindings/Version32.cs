namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    public enum Version32
    {
        /// <summary>
        /// Original was GL_CONTEXT_CORE_PROFILE_BIT = 0x00000001
        /// </summary>
        ContextCoreProfileBit = 1,
        /// <summary>
        /// Original was GL_SYNC_FLUSH_COMMANDS_BIT = 0x00000001
        /// </summary>
        SyncFlushCommandsBit = 1,
        /// <summary>
        /// Original was GL_CONTEXT_COMPATIBILITY_PROFILE_BIT = 0x00000002
        /// </summary>
        ContextCompatibilityProfileBit = 2,
        /// <summary>
        /// Original was GL_LINES_ADJACENCY = 0x000A
        /// </summary>
        LinesAdjacency = 10,
        /// <summary>
        /// Original was GL_LINE_STRIP_ADJACENCY = 0x000B
        /// </summary>
        LineStripAdjacency = 11,
        /// <summary>
        /// Original was GL_TRIANGLES_ADJACENCY = 0x000C
        /// </summary>
        TrianglesAdjacency = 12,
        /// <summary>
        /// Original was GL_TRIANGLE_STRIP_ADJACENCY = 0x000D
        /// </summary>
        TriangleStripAdjacency = 13,
        /// <summary>
        /// Original was GL_PROGRAM_POINT_SIZE = 0x8642
        /// </summary>
        ProgramPointSize = 34370,
        /// <summary>
        /// Original was GL_DEPTH_CLAMP = 0x864F
        /// </summary>
        DepthClamp = 34383,
        /// <summary>
        /// Original was GL_TEXTURE_CUBE_MAP_SEAMLESS = 0x884F
        /// </summary>
        TextureCubeMapSeamless = 34895,
        /// <summary>
        /// Original was GL_GEOMETRY_VERTICES_OUT = 0x8916
        /// </summary>
        GeometryVerticesOut = 35094,
        /// <summary>
        /// Original was GL_GEOMETRY_INPUT_TYPE = 0x8917
        /// </summary>
        GeometryInputType = 35095,
        /// <summary>
        /// Original was GL_GEOMETRY_OUTPUT_TYPE = 0x8918
        /// </summary>
        GeometryOutputType = 35096,
        /// <summary>
        /// Original was GL_MAX_GEOMETRY_TEXTURE_IMAGE_UNITS = 0x8C29
        /// </summary>
        MaxGeometryTextureImageUnits = 35881,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_ATTACHMENT_LAYERED = 0x8DA7
        /// </summary>
        FramebufferAttachmentLayered = 36263,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_INCOMPLETE_LAYER_TARGETS = 0x8DA8
        /// </summary>
        FramebufferIncompleteLayerTargets = 36264,
        /// <summary>
        /// Original was GL_GEOMETRY_SHADER = 0x8DD9
        /// </summary>
        GeometryShader = 36313,
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
        /// Original was GL_QUADS_FOLLOW_PROVOKING_VERTEX_CONVENTION = 0x8E4C
        /// </summary>
        QuadsFollowProvokingVertexConvention = 36428,
        /// <summary>
        /// Original was GL_FIRST_VERTEX_CONVENTION = 0x8E4D
        /// </summary>
        FirstVertexConvention = 36429,
        /// <summary>
        /// Original was GL_LAST_VERTEX_CONVENTION = 0x8E4E
        /// </summary>
        LastVertexConvention = 36430,
        /// <summary>
        /// Original was GL_PROVOKING_VERTEX = 0x8E4F
        /// </summary>
        ProvokingVertex = 36431,
        /// <summary>
        /// Original was GL_SAMPLE_POSITION = 0x8E50
        /// </summary>
        SamplePosition = 36432,
        /// <summary>
        /// Original was GL_SAMPLE_MASK = 0x8E51
        /// </summary>
        SampleMask = 36433,
        /// <summary>
        /// Original was GL_SAMPLE_MASK_VALUE = 0x8E52
        /// </summary>
        SampleMaskValue = 36434,
        /// <summary>
        /// Original was GL_MAX_SAMPLE_MASK_WORDS = 0x8E59
        /// </summary>
        MaxSampleMaskWords = 36441,
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
        ProxyTexture2DMultisampleArray = 37123,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_2D_MULTISAMPLE = 0x9104
        /// </summary>
        TextureBinding2DMultisample = 37124,
        /// <summary>
        /// Original was GL_TEXTURE_BINDING_2D_MULTISAMPLE_ARRAY = 0x9105
        /// </summary>
        TextureBinding2DMultisampleArray = 37125,
        /// <summary>
        /// Original was GL_TEXTURE_SAMPLES = 0x9106
        /// </summary>
        TextureSamples = 37126,
        /// <summary>
        /// Original was GL_TEXTURE_FIXED_SAMPLE_LOCATIONS = 0x9107
        /// </summary>
        TextureFixedSampleLocations = 37127,
        /// <summary>
        /// Original was GL_SAMPLER_2D_MULTISAMPLE = 0x9108
        /// </summary>
        Sampler2DMultisample = 37128,
        /// <summary>
        /// Original was GL_INT_SAMPLER_2D_MULTISAMPLE = 0x9109
        /// </summary>
        IntSampler2DMultisample = 37129,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE = 0x910A
        /// </summary>
        UnsignedIntSampler2DMultisample = 37130,
        /// <summary>
        /// Original was GL_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910B
        /// </summary>
        Sampler2DMultisampleArray = 37131,
        /// <summary>
        /// Original was GL_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910C
        /// </summary>
        IntSampler2DMultisampleArray = 37132,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_2D_MULTISAMPLE_ARRAY = 0x910D
        /// </summary>
        UnsignedIntSampler2DMultisampleArray = 37133,
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
        /// Original was GL_MAX_SERVER_WAIT_TIMEOUT = 0x9111
        /// </summary>
        MaxServerWaitTimeout = 37137,
        /// <summary>
        /// Original was GL_OBJECT_TYPE = 0x9112
        /// </summary>
        ObjectType = 37138,
        /// <summary>
        /// Original was GL_SYNC_CONDITION = 0x9113
        /// </summary>
        SyncCondition = 37139,
        /// <summary>
        /// Original was GL_SYNC_STATUS = 0x9114
        /// </summary>
        SyncStatus = 37140,
        /// <summary>
        /// Original was GL_SYNC_FLAGS = 0x9115
        /// </summary>
        SyncFlags = 37141,
        /// <summary>
        /// Original was GL_SYNC_FENCE = 0x9116
        /// </summary>
        SyncFence = 37142,
        /// <summary>
        /// Original was GL_SYNC_GPU_COMMANDS_COMPLETE = 0x9117
        /// </summary>
        SyncGpuCommandsComplete = 37143,
        /// <summary>
        /// Original was GL_UNSIGNALED = 0x9118
        /// </summary>
        Unsignaled = 37144,
        /// <summary>
        /// Original was GL_SIGNALED = 0x9119
        /// </summary>
        Signaled = 37145,
        /// <summary>
        /// Original was GL_ALREADY_SIGNALED = 0x911A
        /// </summary>
        AlreadySignaled = 37146,
        /// <summary>
        /// Original was GL_TIMEOUT_EXPIRED = 0x911B
        /// </summary>
        TimeoutExpired = 37147,
        /// <summary>
        /// Original was GL_CONDITION_SATISFIED = 0x911C
        /// </summary>
        ConditionSatisfied = 37148,
        /// <summary>
        /// Original was GL_WAIT_FAILED = 0x911D
        /// </summary>
        WaitFailed = 37149,
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
        /// Original was GL_CONTEXT_PROFILE_MASK = 0x9126
        /// </summary>
        ContextProfileMask = 37158,
        /// <summary>
        /// Original was GL_TIMEOUT_IGNORED = 0xFFFFFFFFFFFFFFFF
        /// </summary>
        TimeoutIgnored = -1
    }
}