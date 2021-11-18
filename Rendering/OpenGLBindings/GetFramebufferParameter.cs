namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Ext.GetFramebufferParameter, GL.Ext.GetNamedFramebufferParameter
    /// </summary>
    public enum GetFramebufferParameter
    {
        /// <summary>
        /// Original was GL_DOUBLEBUFFER = 0x0C32
        /// </summary>
        Doublebuffer = 3122,
        /// <summary>
        /// Original was GL_STEREO = 0x0C33
        /// </summary>
        Stereo = 3123,
        /// <summary>
        /// Original was GL_SAMPLE_BUFFERS = 0x80A8
        /// </summary>
        SampleBuffers = 32936,
        /// <summary>
        /// Original was GL_SAMPLES = 0x80A9
        /// </summary>
        Samples = 32937,
        /// <summary>
        /// Original was GL_IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A
        /// </summary>
        ImplementationColorReadType = 35738,
        /// <summary>
        /// Original was GL_IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B
        /// </summary>
        ImplementationColorReadFormat = 35739,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_DEFAULT_WIDTH = 0x9310
        /// </summary>
        FramebufferDefaultWidth = 37648,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_DEFAULT_HEIGHT = 0x9311
        /// </summary>
        FramebufferDefaultHeight = 37649,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_DEFAULT_LAYERS = 0x9312
        /// </summary>
        FramebufferDefaultLayers = 37650,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_DEFAULT_SAMPLES = 0x9313
        /// </summary>
        FramebufferDefaultSamples = 37651,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_DEFAULT_FIXED_SAMPLE_LOCATIONS = 0x9314
        /// </summary>
        FramebufferDefaultFixedSampleLocations = 37652
    }
}