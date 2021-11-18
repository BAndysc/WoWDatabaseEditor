namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.BeginQuery, GL.BeginQueryIndexed and 5 other functions
    /// </summary>
    public enum QueryTarget
    {
        /// <summary>
        /// Original was GL_TIME_ELAPSED = 0x88BF
        /// </summary>
        TimeElapsed = 35007,
        /// <summary>
        /// Original was GL_SAMPLES_PASSED = 0x8914
        /// </summary>
        SamplesPassed = 35092,
        /// <summary>
        /// Original was GL_ANY_SAMPLES_PASSED = 0x8C2F
        /// </summary>
        AnySamplesPassed = 35887,
        /// <summary>
        /// Original was GL_PRIMITIVES_GENERATED = 0x8C87
        /// </summary>
        PrimitivesGenerated = 35975,
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN = 0x8C88
        /// </summary>
        TransformFeedbackPrimitivesWritten = 35976,
        /// <summary>
        /// Original was GL_ANY_SAMPLES_PASSED_CONSERVATIVE = 0x8D6A
        /// </summary>
        AnySamplesPassedConservative = 36202,
        /// <summary>
        /// Original was GL_TIMESTAMP = 0x8E28
        /// </summary>
        Timestamp = 36392
    }
}