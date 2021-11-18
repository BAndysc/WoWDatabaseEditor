namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.GetnColorTable, GL.ColorSubTable and 7 other functions
    /// </summary>
    public enum ColorTableTarget
    {
        /// <summary>
        /// Original was GL_COLOR_TABLE = 0x80D0
        /// </summary>
        ColorTable = 32976,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_COLOR_TABLE = 0x80D1
        /// </summary>
        PostConvolutionColorTable,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D2
        /// </summary>
        PostColorMatrixColorTable,
        /// <summary>
        /// Original was GL_PROXY_COLOR_TABLE = 0x80D3
        /// </summary>
        ProxyColorTable,
        /// <summary>
        /// Original was GL_PROXY_POST_CONVOLUTION_COLOR_TABLE = 0x80D4
        /// </summary>
        ProxyPostConvolutionColorTable,
        /// <summary>
        /// Original was GL_PROXY_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D5
        /// </summary>
        ProxyPostColorMatrixColorTable
    }
}