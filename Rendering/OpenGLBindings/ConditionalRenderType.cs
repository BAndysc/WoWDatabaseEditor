namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.BeginConditionalRender
    /// </summary>
    public enum ConditionalRenderType
    {
        /// <summary>
        /// Original was GL_QUERY_WAIT = 0x8E13
        /// </summary>
        QueryWait = 36371,
        /// <summary>
        /// Original was GL_QUERY_NO_WAIT = 0x8E14
        /// </summary>
        QueryNoWait,
        /// <summary>
        /// Original was GL_QUERY_BY_REGION_WAIT = 0x8E15
        /// </summary>
        QueryByRegionWait,
        /// <summary>
        /// Original was GL_QUERY_BY_REGION_NO_WAIT = 0x8E16
        /// </summary>
        QueryByRegionNoWait,
        /// <summary>
        /// Original was GL_QUERY_WAIT_INVERTED = 0x8E17
        /// </summary>
        QueryWaitInverted,
        /// <summary>
        /// Original was GL_QUERY_NO_WAIT_INVERTED = 0x8E18
        /// </summary>
        QueryNoWaitInverted,
        /// <summary>
        /// Original was GL_QUERY_BY_REGION_WAIT_INVERTED = 0x8E19
        /// </summary>
        QueryByRegionWaitInverted,
        /// <summary>
        /// Original was GL_QUERY_BY_REGION_NO_WAIT_INVERTED = 0x8E1A
        /// </summary>
        QueryByRegionNoWaitInverted
    }
}