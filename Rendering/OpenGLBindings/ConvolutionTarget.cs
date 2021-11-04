namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.GetnConvolutionFilter, GL.ConvolutionFilter1D and 7 other functions
    /// </summary>
    public enum ConvolutionTarget
    {
        /// <summary>
        /// Original was GL_CONVOLUTION_1D = 0x8010
        /// </summary>
        Convolution1D = 32784,
        /// <summary>
        /// Original was GL_CONVOLUTION_2D = 0x8011
        /// </summary>
        Convolution2D,
        /// <summary>
        /// Original was GL_SEPARABLE_2D = 0x8012
        /// </summary>
        Separable2D
    }
}