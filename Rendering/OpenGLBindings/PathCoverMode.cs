namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.NV.CoverFillPathInstanced, GL.NV.CoverFillPath and 2 other functions
    /// </summary>
    public enum PathCoverMode
    {
        /// <summary>
        /// Original was GL_PATH_FILL_COVER_MODE_NV = 0x9082
        /// </summary>
        PathFillCoverModeNv = 36994,
        /// <summary>
        /// Original was GL_CONVEX_HULL_NV = 0x908B
        /// </summary>
        ConvexHullNv = 37003,
        /// <summary>
        /// Original was GL_BOUNDING_BOX_NV = 0x908D
        /// </summary>
        BoundingBoxNv = 37005,
        /// <summary>
        /// Original was GL_BOUNDING_BOX_OF_BOUNDING_BOXES_NV = 0x909C
        /// </summary>
        BoundingBoxOfBoundingBoxesNv = 37020
    }
}