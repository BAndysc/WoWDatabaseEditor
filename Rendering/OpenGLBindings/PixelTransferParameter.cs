namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    public enum PixelTransferParameter
    {
        /// <summary>
        /// Original was GL_MAP_COLOR = 0x0D10
        /// </summary>
        MapColor = 3344,
        /// <summary>
        /// Original was GL_MAP_STENCIL = 0x0D11
        /// </summary>
        MapStencil = 3345,
        /// <summary>
        /// Original was GL_INDEX_SHIFT = 0x0D12
        /// </summary>
        IndexShift = 3346,
        /// <summary>
        /// Original was GL_INDEX_OFFSET = 0x0D13
        /// </summary>
        IndexOffset = 3347,
        /// <summary>
        /// Original was GL_RED_SCALE = 0x0D14
        /// </summary>
        RedScale = 3348,
        /// <summary>
        /// Original was GL_RED_BIAS = 0x0D15
        /// </summary>
        RedBias = 3349,
        /// <summary>
        /// Original was GL_GREEN_SCALE = 0x0D18
        /// </summary>
        GreenScale = 3352,
        /// <summary>
        /// Original was GL_GREEN_BIAS = 0x0D19
        /// </summary>
        GreenBias = 3353,
        /// <summary>
        /// Original was GL_BLUE_SCALE = 0x0D1A
        /// </summary>
        BlueScale = 3354,
        /// <summary>
        /// Original was GL_BLUE_BIAS = 0x0D1B
        /// </summary>
        BlueBias = 3355,
        /// <summary>
        /// Original was GL_ALPHA_SCALE = 0x0D1C
        /// </summary>
        AlphaScale = 3356,
        /// <summary>
        /// Original was GL_ALPHA_BIAS = 0x0D1D
        /// </summary>
        AlphaBias = 3357,
        /// <summary>
        /// Original was GL_DEPTH_SCALE = 0x0D1E
        /// </summary>
        DepthScale = 3358,
        /// <summary>
        /// Original was GL_DEPTH_BIAS = 0x0D1F
        /// </summary>
        DepthBias = 3359,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_RED_SCALE = 0x801C
        /// </summary>
        PostConvolutionRedScale = 32796,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_RED_SCALE_EXT = 0x801C
        /// </summary>
        PostConvolutionRedScaleExt = 32796,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_GREEN_SCALE = 0x801D
        /// </summary>
        PostConvolutionGreenScale = 32797,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_GREEN_SCALE_EXT = 0x801D
        /// </summary>
        PostConvolutionGreenScaleExt = 32797,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_BLUE_SCALE = 0x801E
        /// </summary>
        PostConvolutionBlueScale = 32798,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_BLUE_SCALE_EXT = 0x801E
        /// </summary>
        PostConvolutionBlueScaleExt = 32798,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_ALPHA_SCALE = 0x801F
        /// </summary>
        PostConvolutionAlphaScale = 32799,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_ALPHA_SCALE_EXT = 0x801F
        /// </summary>
        PostConvolutionAlphaScaleExt = 32799,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_RED_BIAS = 0x8020
        /// </summary>
        PostConvolutionRedBias = 32800,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_RED_BIAS_EXT = 0x8020
        /// </summary>
        PostConvolutionRedBiasExt = 32800,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_GREEN_BIAS = 0x8021
        /// </summary>
        PostConvolutionGreenBias = 32801,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_GREEN_BIAS_EXT = 0x8021
        /// </summary>
        PostConvolutionGreenBiasExt = 32801,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_BLUE_BIAS = 0x8022
        /// </summary>
        PostConvolutionBlueBias = 32802,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_BLUE_BIAS_EXT = 0x8022
        /// </summary>
        PostConvolutionBlueBiasExt = 32802,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_ALPHA_BIAS = 0x8023
        /// </summary>
        PostConvolutionAlphaBias = 32803,
        /// <summary>
        /// Original was GL_POST_CONVOLUTION_ALPHA_BIAS_EXT = 0x8023
        /// </summary>
        PostConvolutionAlphaBiasExt = 32803,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_RED_SCALE = 0x80B4
        /// </summary>
        PostColorMatrixRedScale = 32948,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_RED_SCALE_SGI = 0x80B4
        /// </summary>
        PostColorMatrixRedScaleSgi = 32948,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_GREEN_SCALE = 0x80B5
        /// </summary>
        PostColorMatrixGreenScale = 32949,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_GREEN_SCALE_SGI = 0x80B5
        /// </summary>
        PostColorMatrixGreenScaleSgi = 32949,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_BLUE_SCALE = 0x80B6
        /// </summary>
        PostColorMatrixBlueScale = 32950,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_BLUE_SCALE_SGI = 0x80B6
        /// </summary>
        PostColorMatrixBlueScaleSgi = 32950,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_ALPHA_SCALE = 0x80B7
        /// </summary>
        PostColorMatrixAlphaScale = 32951,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_ALPHA_SCALE_SGI = 0x80B7
        /// </summary>
        PostColorMatrixAlphaScaleSgi = 32951,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_RED_BIAS = 0x80B8
        /// </summary>
        PostColorMatrixRedBias = 32952,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_RED_BIAS_SGI = 0x80B8
        /// </summary>
        PostColorMatrixRedBiasSgi = 32952,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_GREEN_BIAS = 0x80B9
        /// </summary>
        PostColorMatrixGreenBias = 32953,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_GREEN_BIAS_SGI = 0x80B9
        /// </summary>
        PostColorMatrixGreenBiasSgi = 32953,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_BLUE_BIAS = 0x80BA
        /// </summary>
        PostColorMatrixBlueBias = 32954,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_BLUE_BIAS_SGI = 0x80BA
        /// </summary>
        PostColorMatrixBlueBiasSgi = 32954,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_ALPHA_BIAS = 0x80BB
        /// </summary>
        PostColorMatrixAlphaBias = 32955,
        /// <summary>
        /// Original was GL_POST_COLOR_MATRIX_ALPHA_BIAS_SGI = 0x80BB
        /// </summary>
        PostColorMatrixAlphaBiasSgi = 32955
    }
}