namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Ext.GetDouble, GL.Ext.GetDoubleIndexed and 6 other functions
    /// </summary>
    public enum TypeEnum
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
        QueryByRegionNoWait
    }
}