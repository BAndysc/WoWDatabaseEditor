namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    public enum BeginMode
    {
        /// <summary>
        /// Original was GL_POINTS = 0x0000
        /// </summary>
        Points = 0,
        /// <summary>
        /// Original was GL_LINES = 0x0001
        /// </summary>
        Lines = 1,
        /// <summary>
        /// Original was GL_LINE_LOOP = 0x0002
        /// </summary>
        LineLoop = 2,
        /// <summary>
        /// Original was GL_LINE_STRIP = 0x0003
        /// </summary>
        LineStrip = 3,
        /// <summary>
        /// Original was GL_TRIANGLES = 0x0004
        /// </summary>
        Triangles = 4,
        /// <summary>
        /// Original was GL_TRIANGLE_STRIP = 0x0005
        /// </summary>
        TriangleStrip = 5,
        /// <summary>
        /// Original was GL_TRIANGLE_FAN = 0x0006
        /// </summary>
        TriangleFan = 6,
        /// <summary>
        /// Original was GL_QUADS = 0x0007
        /// </summary>
        Quads = 7,
        /// <summary>
        /// Original was GL_QUAD_STRIP = 0x0008
        /// </summary>
        QuadStrip = 8,
        /// <summary>
        /// Original was GL_POLYGON = 0x0009
        /// </summary>
        Polygon = 9,
        /// <summary>
        /// Original was GL_PATCHES = 0x000E
        /// </summary>
        Patches = 14,
        /// <summary>
        /// Original was GL_LINES_ADJACENCY = 0xA
        /// </summary>
        LinesAdjacency = 10,
        /// <summary>
        /// Original was GL_LINE_STRIP_ADJACENCY = 0xB
        /// </summary>
        LineStripAdjacency = 11,
        /// <summary>
        /// Original was GL_TRIANGLES_ADJACENCY = 0xC
        /// </summary>
        TrianglesAdjacency = 12,
        /// <summary>
        /// Original was GL_TRIANGLE_STRIP_ADJACENCY = 0xD
        /// </summary>
        TriangleStripAdjacency = 13
    }
}