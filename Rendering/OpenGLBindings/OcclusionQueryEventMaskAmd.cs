using System;

namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    [Flags]
    public enum OcclusionQueryEventMaskAmd
    {
        /// <summary>
        /// Original was GL_QUERY_DEPTH_PASS_EVENT_BIT_AMD = 0x00000001
        /// </summary>
        QueryDepthPassEventBitAmd = 0x1,
        /// <summary>
        /// Original was GL_QUERY_DEPTH_FAIL_EVENT_BIT_AMD = 0x00000002
        /// </summary>
        QueryDepthFailEventBitAmd = 0x2,
        /// <summary>
        /// Original was GL_QUERY_STENCIL_FAIL_EVENT_BIT_AMD = 0x00000004
        /// </summary>
        QueryStencilFailEventBitAmd = 0x4,
        /// <summary>
        /// Original was GL_QUERY_DEPTH_BOUNDS_FAIL_EVENT_BIT_AMD = 0x00000008
        /// </summary>
        QueryDepthBoundsFailEventBitAmd = 0x8,
        /// <summary>
        /// Original was GL_QUERY_ALL_EVENT_BITS_AMD = 0xFFFFFFFF
        /// </summary>
        QueryAllEventBitsAmd = -1
    }
}