namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GetActiveUniform
    /// </summary>
    public enum ActiveUniformType
    {
        /// <summary>
        /// Original was GL_INT = 0x1404
        /// </summary>
        Int = 5124,
        /// <summary>
        /// Original was GL_UNSIGNED_INT = 0x1405
        /// </summary>
        UnsignedInt = 5125,
        /// <summary>
        /// Original was GL_FLOAT = 0x1406
        /// </summary>
        Float = 5126,
        /// <summary>
        /// Original was GL_DOUBLE = 0x140A
        /// </summary>
        Double = 5130,
        /// <summary>
        /// Original was GL_FLOAT_VEC2 = 0x8B50
        /// </summary>
        FloatVec2 = 35664,
        /// <summary>
        /// Original was GL_FLOAT_VEC3 = 0x8B51
        /// </summary>
        FloatVec3 = 35665,
        /// <summary>
        /// Original was GL_FLOAT_VEC4 = 0x8B52
        /// </summary>
        FloatVec4 = 35666,
        /// <summary>
        /// Original was GL_INT_VEC2 = 0x8B53
        /// </summary>
        IntVec2 = 35667,
        /// <summary>
        /// Original was GL_INT_VEC3 = 0x8B54
        /// </summary>
        IntVec3 = 35668,
        /// <summary>
        /// Original was GL_INT_VEC4 = 0x8B55
        /// </summary>
        IntVec4 = 35669,
        /// <summary>
        /// Original was GL_BOOL = 0x8B56
        /// </summary>
        Bool = 35670,
        /// <summary>
        /// Original was GL_BOOL_VEC2 = 0x8B57
        /// </summary>
        BoolVec2 = 35671,
        /// <summary>
        /// Original was GL_BOOL_VEC3 = 0x8B58
        /// </summary>
        BoolVec3 = 35672,
        /// <summary>
        /// Original was GL_BOOL_VEC4 = 0x8B59
        /// </summary>
        BoolVec4 = 35673,
        /// <summary>
        /// Original was GL_FLOAT_MAT2 = 0x8B5A
        /// </summary>
        FloatMat2 = 35674,
        /// <summary>
        /// Original was GL_FLOAT_MAT3 = 0x8B5B
        /// </summary>
        FloatMat3 = 35675,
        /// <summary>
        /// Original was GL_FLOAT_MAT4 = 0x8B5C
        /// </summary>
        FloatMat4 = 35676,
        /// <summary>
        /// Original was GL_SAMPLER_1D = 0x8B5D
        /// </summary>
        Sampler1D = 35677,
        /// <summary>
        /// Original was GL_SAMPLER_2D = 0x8B5E
        /// </summary>
        Sampler2D = 35678,
        /// <summary>
        /// Original was GL_SAMPLER_3D = 0x8B5F
        /// </summary>
        Sampler3D = 35679,
        /// <summary>
        /// Original was GL_SAMPLER_CUBE = 0x8B60
        /// </summary>
        SamplerCube = 35680,
        /// <summary>
        /// Original was GL_SAMPLER_1D_SHADOW = 0x8B61
        /// </summary>
        Sampler1DShadow = 35681,
        /// <summary>
        /// Original was GL_SAMPLER_2D_SHADOW = 0x8B62
        /// </summary>
        Sampler2DShadow = 35682,
        /// <summary>
        /// Original was GL_SAMPLER_2D_RECT = 0x8B63
        /// </summary>
        Sampler2DRect = 35683,
        /// <summary>
        /// Original was GL_SAMPLER_2D_RECT_SHADOW = 0x8B64
        /// </summary>
        Sampler2DRectShadow = 35684,
        /// <summary>
        /// Original was GL_FLOAT_MAT2x3 = 0x8B65
        /// </summary>
        FloatMat2x3 = 35685,
        /// <summary>
        /// Original was GL_FLOAT_MAT2x4 = 0x8B66
        /// </summary>
        FloatMat2x4 = 35686,
        /// <summary>
        /// Original was GL_FLOAT_MAT3x2 = 0x8B67
        /// </summary>
        FloatMat3x2 = 35687,
        /// <summary>
        /// Original was GL_FLOAT_MAT3x4 = 0x8B68
        /// </summary>
        FloatMat3x4 = 35688,
        /// <summary>
        /// Original was GL_FLOAT_MAT4x2 = 0x8B69
        /// </summary>
        FloatMat4x2 = 35689,
        /// <summary>
        /// Original was GL_FLOAT_MAT4x3 = 0x8B6A
        /// </summary>
        FloatMat4x3 = 35690,
        /// <summary>
        /// Original was GL_SAMPLER_1D_ARRAY = 0x8DC0
        /// </summary>
        Sampler1DArray = 36288,
        /// <summary>
        /// Original was GL_SAMPLER_2D_ARRAY = 0x8DC1
        /// </summary>
        Sampler2DArray = 36289,
        /// <summary>
        /// Original was GL_SAMPLER_BUFFER = 0x8DC2
        /// </summary>
        SamplerBuffer = 36290,
        /// <summary>
        /// Original was GL_SAMPLER_1D_ARRAY_SHADOW = 0x8DC3
        /// </summary>
        Sampler1DArrayShadow = 36291,
        /// <summary>
        /// Original was GL_SAMPLER_2D_ARRAY_SHADOW = 0x8DC4
        /// </summary>
        Sampler2DArrayShadow = 36292,
        /// <summary>
        /// Original was GL_SAMPLER_CUBE_SHADOW = 0x8DC5
        /// </summary>
        SamplerCubeShadow = 36293,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_VEC2 = 0x8DC6
        /// </summary>
        UnsignedIntVec2 = 36294,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_VEC3 = 0x8DC7
        /// </summary>
        UnsignedIntVec3 = 36295,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_VEC4 = 0x8DC8
        /// </summary>
        UnsignedIntVec4 = 36296,
        /// <summary>
        /// Original was GL_INT_SAMPLER_1D = 0x8DC9
        /// </summary>
        IntSampler1D = 36297,
        /// <summary>
        /// Original was GL_INT_SAMPLER_2D = 0x8DCA
        /// </summary>
        IntSampler2D = 36298,
        /// <summary>
        /// Original was GL_INT_SAMPLER_3D = 0x8DCB
        /// </summary>
        IntSampler3D = 36299,
        /// <summary>
        /// Original was GL_INT_SAMPLER_CUBE = 0x8DCC
        /// </summary>
        IntSamplerCube = 36300,
        /// <summary>
        /// Original was GL_INT_SAMPLER_2D_RECT = 0x8DCD
        /// </summary>
        IntSampler2DRect = 36301,
        /// <summary>
        /// Original was GL_INT_SAMPLER_1D_ARRAY = 0x8DCE
        /// </summary>
        IntSampler1DArray = 36302,
        /// <summary>
        /// Original was GL_INT_SAMPLER_2D_ARRAY = 0x8DCF
        /// </summary>
        IntSampler2DArray = 36303,
        /// <summary>
        /// Original was GL_INT_SAMPLER_BUFFER = 0x8DD0
        /// </summary>
        IntSamplerBuffer = 36304,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_1D = 0x8DD1
        /// </summary>
        UnsignedIntSampler1D = 36305,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_2D = 0x8DD2
        /// </summary>
        UnsignedIntSampler2D = 36306,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_3D = 0x8DD3
        /// </summary>
        UnsignedIntSampler3D = 36307,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_CUBE = 0x8DD4
        /// </summary>
        UnsignedIntSamplerCube = 36308,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_2D_RECT = 0x8DD5
        /// </summary>
        UnsignedIntSampler2DRect = 36309,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_1D_ARRAY = 0x8DD6
        /// </summary>
        UnsignedIntSampler1DArray = 36310,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_2D_ARRAY = 0x8DD7
        /// </summary>
        UnsignedIntSampler2DArray = 36311,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_BUFFER = 0x8DD8
        /// </summary>
        UnsignedIntSamplerBuffer = 36312,
        /// <summary>
        /// Original was GL_DOUBLE_VEC2 = 0x8FFC
        /// </summary>
        DoubleVec2 = 36860,
        /// <summary>
        /// Original was GL_DOUBLE_VEC3 = 0x8FFD
        /// </summary>
        DoubleVec3 = 36861,
        /// <summary>
        /// Original was GL_DOUBLE_VEC4 = 0x8FFE
        /// </summary>
        DoubleVec4 = 36862,
        /// <summary>
        /// Original was GL_SAMPLER_CUBE_MAP_ARRAY = 0x900C
        /// </summary>
        SamplerCubeMapArray = 36876,
        /// <summary>
        /// Original was GL_SAMPLER_CUBE_MAP_ARRAY_SHADOW = 0x900D
        /// </summary>
        SamplerCubeMapArrayShadow = 36877,
        /// <summary>
        /// Original was GL_INT_SAMPLER_CUBE_MAP_ARRAY = 0x900E
        /// </summary>
        IntSamplerCubeMapArray = 36878,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_SAMPLER_CUBE_MAP_ARRAY = 0x900F
        /// </summary>
        UnsignedIntSamplerCubeMapArray = 36879,
        /// <summary>
        /// Original was GL_IMAGE_1D = 0x904C
        /// </summary>
        Image1D = 36940,
        /// <summary>
        /// Original was GL_IMAGE_2D = 0x904D
        /// </summary>
        Image2D = 36941,
        /// <summary>
        /// Original was GL_IMAGE_3D = 0x904E
        /// </summary>
        Image3D = 36942,
        /// <summary>
        /// Original was GL_IMAGE_2D_RECT = 0x904F
        /// </summary>
        Image2DRect = 36943,
        /// <summary>
        /// Original was GL_IMAGE_CUBE = 0x9050
        /// </summary>
        ImageCube = 36944,
        /// <summary>
        /// Original was GL_IMAGE_BUFFER = 0x9051
        /// </summary>
        ImageBuffer = 36945,
        /// <summary>
        /// Original was GL_IMAGE_1D_ARRAY = 0x9052
        /// </summary>
        Image1DArray = 36946,
        /// <summary>
        /// Original was GL_IMAGE_2D_ARRAY = 0x9053
        /// </summary>
        Image2DArray = 36947,
        /// <summary>
        /// Original was GL_IMAGE_CUBE_MAP_ARRAY = 0x9054
        /// </summary>
        ImageCubeMapArray = 36948,
        /// <summary>
        /// Original was GL_IMAGE_2D_MULTISAMPLE = 0x9055
        /// </summary>
        Image2DMultisample = 36949,
        /// <summary>
        /// Original was GL_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9056
        /// </summary>
        Image2DMultisampleArray = 36950,
        /// <summary>
        /// Original was GL_INT_IMAGE_1D = 0x9057
        /// </summary>
        IntImage1D = 36951,
        /// <summary>
        /// Original was GL_INT_IMAGE_2D = 0x9058
        /// </summary>
        IntImage2D = 36952,
        /// <summary>
        /// Original was GL_INT_IMAGE_3D = 0x9059
        /// </summary>
        IntImage3D = 36953,
        /// <summary>
        /// Original was GL_INT_IMAGE_2D_RECT = 0x905A
        /// </summary>
        IntImage2DRect = 36954,
        /// <summary>
        /// Original was GL_INT_IMAGE_CUBE = 0x905B
        /// </summary>
        IntImageCube = 36955,
        /// <summary>
        /// Original was GL_INT_IMAGE_BUFFER = 0x905C
        /// </summary>
        IntImageBuffer = 36956,
        /// <summary>
        /// Original was GL_INT_IMAGE_1D_ARRAY = 0x905D
        /// </summary>
        IntImage1DArray = 36957,
        /// <summary>
        /// Original was GL_INT_IMAGE_2D_ARRAY = 0x905E
        /// </summary>
        IntImage2DArray = 36958,
        /// <summary>
        /// Original was GL_INT_IMAGE_CUBE_MAP_ARRAY = 0x905F
        /// </summary>
        IntImageCubeMapArray = 36959,
        /// <summary>
        /// Original was GL_INT_IMAGE_2D_MULTISAMPLE = 0x9060
        /// </summary>
        IntImage2DMultisample = 36960,
        /// <summary>
        /// Original was GL_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x9061
        /// </summary>
        IntImage2DMultisampleArray = 36961,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_1D = 0x9062
        /// </summary>
        UnsignedIntImage1D = 36962,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_2D = 0x9063
        /// </summary>
        UnsignedIntImage2D = 36963,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_3D = 0x9064
        /// </summary>
        UnsignedIntImage3D = 36964,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_2D_RECT = 0x9065
        /// </summary>
        UnsignedIntImage2DRect = 36965,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_CUBE = 0x9066
        /// </summary>
        UnsignedIntImageCube = 36966,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_BUFFER = 0x9067
        /// </summary>
        UnsignedIntImageBuffer = 36967,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_1D_ARRAY = 0x9068
        /// </summary>
        UnsignedIntImage1DArray = 36968,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_2D_ARRAY = 0x9069
        /// </summary>
        UnsignedIntImage2DArray = 36969,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_CUBE_MAP_ARRAY = 0x906A
        /// </summary>
        UnsignedIntImageCubeMapArray = 36970,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE = 0x906B
        /// </summary>
        UnsignedIntImage2DMultisample = 36971,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_IMAGE_2D_MULTISAMPLE_ARRAY = 0x906C
        /// </summary>
        UnsignedIntImage2DMultisampleArray = 36972,
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
        /// Original was GL_UNSIGNED_INT_ATOMIC_COUNTER = 0x92DB
        /// </summary>
        UnsignedIntAtomicCounter = 37595
    }
}