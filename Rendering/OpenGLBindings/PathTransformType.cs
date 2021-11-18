namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.NV.CoverFillPathInstanced, GL.NV.CoverStrokePathInstanced and 4 other functions
    /// </summary>
    public enum PathTransformType
    {
        /// <summary>
        /// Original was GL_NONE = 0
        /// </summary>
        None = 0,
        /// <summary>
        /// Original was GL_TRANSLATE_X_NV = 0x908E
        /// </summary>
        TranslateXNv = 37006,
        /// <summary>
        /// Original was GL_TRANSLATE_Y_NV = 0x908F
        /// </summary>
        TranslateYNv = 37007,
        /// <summary>
        /// Original was GL_TRANSLATE_2D_NV = 0x9090
        /// </summary>
        Translate2DNv = 37008,
        /// <summary>
        /// Original was GL_TRANSLATE_3D_NV = 0x9091
        /// </summary>
        Translate3DNv = 37009,
        /// <summary>
        /// Original was GL_AFFINE_2D_NV = 0x9092
        /// </summary>
        Affine2DNv = 37010,
        /// <summary>
        /// Original was GL_AFFINE_3D_NV = 0x9094
        /// </summary>
        Affine3DNv = 37012,
        /// <summary>
        /// Original was GL_TRANSPOSE_AFFINE_2D_NV = 0x9096
        /// </summary>
        TransposeAffine2DNv = 37014,
        /// <summary>
        /// Original was GL_TRANSPOSE_AFFINE_3D_NV = 0x9098
        /// </summary>
        TransposeAffine3DNv = 37016
    }
}