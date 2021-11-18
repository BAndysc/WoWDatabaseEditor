namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GetProgramInterface, GL.GetProgramResourceIndex and 5 other functions
    /// </summary>
    public enum ProgramInterface
    {
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_BUFFER = 0x8C8E
        /// </summary>
        TransformFeedbackBuffer = 35982,
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
        TransformFeedbackVarying = 37620
    }
}