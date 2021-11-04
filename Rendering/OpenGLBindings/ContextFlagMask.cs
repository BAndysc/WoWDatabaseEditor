namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    [Flags]
    public enum ContextFlagMask
    {
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_FORWARD_COMPATIBLE_BIT = 0x00000001
        /// </summary>
        ContextFlagForwardCompatibleBit = 0x1,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_DEBUG_BIT = 0x00000002
        /// </summary>
        ContextFlagDebugBit = 0x2,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_DEBUG_BIT_KHR = 0x00000002
        /// </summary>
        ContextFlagDebugBitKhr = 0x2,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_ROBUST_ACCESS_BIT = 0x00000004
        /// </summary>
        ContextFlagRobustAccessBit = 0x4,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_ROBUST_ACCESS_BIT_ARB = 0x00000004
        /// </summary>
        ContextFlagRobustAccessBitArb = 0x4,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_NO_ERROR_BIT = 0x00000008
        /// </summary>
        ContextFlagNoErrorBit = 0x8,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_NO_ERROR_BIT_KHR = 0x00000008
        /// </summary>
        ContextFlagNoErrorBitKhr = 0x8,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_PROTECTED_CONTENT_BIT_EXT = 0x00000010
        /// </summary>
        ContextFlagProtectedContentBitExt = 0x10
    }
}