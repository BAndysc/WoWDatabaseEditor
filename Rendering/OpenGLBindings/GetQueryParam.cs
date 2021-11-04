namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GetQueryIndexed, GL.GetQuery
    /// </summary>
    public enum GetQueryParam
    {
        /// <summary>
        /// Original was GL_QUERY_COUNTER_BITS = 0x8864
        /// </summary>
        QueryCounterBits = 34916,
        /// <summary>
        /// Original was GL_CURRENT_QUERY = 0x8865
        /// </summary>
        CurrentQuery
    }
}