namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GetQueryObject
    /// </summary>
    public enum GetQueryObjectParam
    {
        /// <summary>
        /// Original was GL_QUERY_TARGET = 0x82EA
        /// </summary>
        QueryTarget = 33514,
        /// <summary>
        /// Original was GL_QUERY_RESULT = 0x8866
        /// </summary>
        QueryResult = 34918,
        /// <summary>
        /// Original was GL_QUERY_RESULT_AVAILABLE = 0x8867
        /// </summary>
        QueryResultAvailable = 34919,
        /// <summary>
        /// Original was GL_QUERY_RESULT_NO_WAIT = 0x9194
        /// </summary>
        QueryResultNoWait = 37268
    }
}