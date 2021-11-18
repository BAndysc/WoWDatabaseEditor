namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GetProgramPipeline
    /// </summary>
    public enum ProgramPipelineParameter
    {
        /// <summary>
        /// Original was GL_ACTIVE_PROGRAM = 0x8259
        /// </summary>
        ActiveProgram = 33369,
        /// <summary>
        /// Original was GL_FRAGMENT_SHADER = 0x8B30
        /// </summary>
        FragmentShader = 35632,
        /// <summary>
        /// Original was GL_VERTEX_SHADER = 0x8B31
        /// </summary>
        VertexShader = 35633,
        /// <summary>
        /// Original was GL_VALIDATE_STATUS = 0x8B83
        /// </summary>
        ValidateStatus = 35715,
        /// <summary>
        /// Original was GL_INFO_LOG_LENGTH = 0x8B84
        /// </summary>
        InfoLogLength = 35716,
        /// <summary>
        /// Original was GL_GEOMETRY_SHADER = 0x8DD9
        /// </summary>
        GeometryShader = 36313,
        /// <summary>
        /// Original was GL_TESS_EVALUATION_SHADER = 0x8E87
        /// </summary>
        TessEvaluationShader = 36487,
        /// <summary>
        /// Original was GL_TESS_CONTROL_SHADER = 0x8E88
        /// </summary>
        TessControlShader = 36488,
        /// <summary>
        /// Original was GL_COMPUTE_SHADER = 0x91B9
        /// </summary>
        ComputeShader = 37305
    }
}