namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    public enum ArbProgramInterfaceQuery
    {
        /// <summary>
        /// Original was GL_NUM_COMPATIBLE_SUBROUTINES = 0x8E4A
        /// </summary>
        NumCompatibleSubroutines = 36426,
        /// <summary>
        /// Original was GL_COMPATIBLE_SUBROUTINES = 0x8E4B
        /// </summary>
        CompatibleSubroutines = 36427,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER = 0x92C0
        /// </summary>
        AtomicCounterBuffer = 37568,
        /// <summary>
        /// Original was GL_UNIFORM = 0x92E1
        /// </summary>
        Uniform = 37601,
        /// <summary>
        /// Original was GL_UNIFORM_BLOCK = 0x92E2
        /// </summary>
        UniformBlock = 37602,
        /// <summary>
        /// Original was GL_PROGRAM_INPUT = 0x92E3
        /// </summary>
        ProgramInput = 37603,
        /// <summary>
        /// Original was GL_PROGRAM_OUTPUT = 0x92E4
        /// </summary>
        ProgramOutput = 37604,
        /// <summary>
        /// Original was GL_BUFFER_VARIABLE = 0x92E5
        /// </summary>
        BufferVariable = 37605,
        /// <summary>
        /// Original was GL_SHADER_STORAGE_BLOCK = 0x92E6
        /// </summary>
        ShaderStorageBlock = 37606,
        /// <summary>
        /// Original was GL_IS_PER_PATCH = 0x92E7
        /// </summary>
        IsPerPatch = 37607,
        /// <summary>
        /// Original was GL_VERTEX_SUBROUTINE = 0x92E8
        /// </summary>
        VertexSubroutine = 37608,
        /// <summary>
        /// Original was GL_TESS_CONTROL_SUBROUTINE = 0x92E9
        /// </summary>
        TessControlSubroutine = 37609,
        /// <summary>
        /// Original was GL_TESS_EVALUATION_SUBROUTINE = 0x92EA
        /// </summary>
        TessEvaluationSubroutine = 37610,
        /// <summary>
        /// Original was GL_GEOMETRY_SUBROUTINE = 0x92EB
        /// </summary>
        GeometrySubroutine = 37611,
        /// <summary>
        /// Original was GL_FRAGMENT_SUBROUTINE = 0x92EC
        /// </summary>
        FragmentSubroutine = 37612,
        /// <summary>
        /// Original was GL_COMPUTE_SUBROUTINE = 0x92ED
        /// </summary>
        ComputeSubroutine = 37613,
        /// <summary>
        /// Original was GL_VERTEX_SUBROUTINE_UNIFORM = 0x92EE
        /// </summary>
        VertexSubroutineUniform = 37614,
        /// <summary>
        /// Original was GL_TESS_CONTROL_SUBROUTINE_UNIFORM = 0x92EF
        /// </summary>
        TessControlSubroutineUniform = 37615,
        /// <summary>
        /// Original was GL_TESS_EVALUATION_SUBROUTINE_UNIFORM = 0x92F0
        /// </summary>
        TessEvaluationSubroutineUniform = 37616,
        /// <summary>
        /// Original was GL_GEOMETRY_SUBROUTINE_UNIFORM = 0x92F1
        /// </summary>
        GeometrySubroutineUniform = 37617,
        /// <summary>
        /// Original was GL_FRAGMENT_SUBROUTINE_UNIFORM = 0x92F2
        /// </summary>
        FragmentSubroutineUniform = 37618,
        /// <summary>
        /// Original was GL_COMPUTE_SUBROUTINE_UNIFORM = 0x92F3
        /// </summary>
        ComputeSubroutineUniform = 37619,
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_VARYING = 0x92F4
        /// </summary>
        TransformFeedbackVarying = 37620,
        /// <summary>
        /// Original was GL_ACTIVE_RESOURCES = 0x92F5
        /// </summary>
        ActiveResources = 37621,
        /// <summary>
        /// Original was GL_MAX_NAME_LENGTH = 0x92F6
        /// </summary>
        MaxNameLength = 37622,
        /// <summary>
        /// Original was GL_MAX_NUM_ACTIVE_VARIABLES = 0x92F7
        /// </summary>
        MaxNumActiveVariables = 37623,
        /// <summary>
        /// Original was GL_MAX_NUM_COMPATIBLE_SUBROUTINES = 0x92F8
        /// </summary>
        MaxNumCompatibleSubroutines = 37624,
        /// <summary>
        /// Original was GL_NAME_LENGTH = 0x92F9
        /// </summary>
        NameLength = 37625,
        /// <summary>
        /// Original was GL_TYPE = 0x92FA
        /// </summary>
        Type = 37626,
        /// <summary>
        /// Original was GL_ARRAY_SIZE = 0x92FB
        /// </summary>
        ArraySize = 37627,
        /// <summary>
        /// Original was GL_OFFSET = 0x92FC
        /// </summary>
        Offset = 37628,
        /// <summary>
        /// Original was GL_BLOCK_INDEX = 0x92FD
        /// </summary>
        BlockIndex = 37629,
        /// <summary>
        /// Original was GL_ARRAY_STRIDE = 0x92FE
        /// </summary>
        ArrayStride = 37630,
        /// <summary>
        /// Original was GL_MATRIX_STRIDE = 0x92FF
        /// </summary>
        MatrixStride = 37631,
        /// <summary>
        /// Original was GL_IS_ROW_MAJOR = 0x9300
        /// </summary>
        IsRowMajor = 37632,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_INDEX = 0x9301
        /// </summary>
        AtomicCounterBufferIndex = 37633,
        /// <summary>
        /// Original was GL_BUFFER_BINDING = 0x9302
        /// </summary>
        BufferBinding = 37634,
        /// <summary>
        /// Original was GL_BUFFER_DATA_SIZE = 0x9303
        /// </summary>
        BufferDataSize = 37635,
        /// <summary>
        /// Original was GL_NUM_ACTIVE_VARIABLES = 0x9304
        /// </summary>
        NumActiveVariables = 37636,
        /// <summary>
        /// Original was GL_ACTIVE_VARIABLES = 0x9305
        /// </summary>
        ActiveVariables = 37637,
        /// <summary>
        /// Original was GL_REFERENCED_BY_VERTEX_SHADER = 0x9306
        /// </summary>
        ReferencedByVertexShader = 37638,
        /// <summary>
        /// Original was GL_REFERENCED_BY_TESS_CONTROL_SHADER = 0x9307
        /// </summary>
        ReferencedByTessControlShader = 37639,
        /// <summary>
        /// Original was GL_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x9308
        /// </summary>
        ReferencedByTessEvaluationShader = 37640,
        /// <summary>
        /// Original was GL_REFERENCED_BY_GEOMETRY_SHADER = 0x9309
        /// </summary>
        ReferencedByGeometryShader = 37641,
        /// <summary>
        /// Original was GL_REFERENCED_BY_FRAGMENT_SHADER = 0x930A
        /// </summary>
        ReferencedByFragmentShader = 37642,
        /// <summary>
        /// Original was GL_REFERENCED_BY_COMPUTE_SHADER = 0x930B
        /// </summary>
        ReferencedByComputeShader = 37643,
        /// <summary>
        /// Original was GL_TOP_LEVEL_ARRAY_SIZE = 0x930C
        /// </summary>
        TopLevelArraySize = 37644,
        /// <summary>
        /// Original was GL_TOP_LEVEL_ARRAY_STRIDE = 0x930D
        /// </summary>
        TopLevelArrayStride = 37645,
        /// <summary>
        /// Original was GL_LOCATION = 0x930E
        /// </summary>
        Location = 37646,
        /// <summary>
        /// Original was GL_LOCATION_INDEX = 0x930F
        /// </summary>
        LocationIndex = 37647
    }
}