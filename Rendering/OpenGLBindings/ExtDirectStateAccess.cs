namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Ext.ClearNamedBufferData, GL.Ext.ClearNamedBufferSubData and 15 other functions
    /// </summary>
    public enum ExtDirectStateAccess
    {
        /// <summary>
        /// Original was GL_PROGRAM_MATRIX_EXT = 0x8E2D
        /// </summary>
        ProgramMatrixExt = 36397,
        /// <summary>
        /// Original was GL_TRANSPOSE_PROGRAM_MATRIX_EXT = 0x8E2E
        /// </summary>
        TransposeProgramMatrixExt,
        /// <summary>
        /// Original was GL_PROGRAM_MATRIX_STACK_DEPTH_EXT = 0x8E2F
        /// </summary>
        ProgramMatrixStackDepthExt
    }
}