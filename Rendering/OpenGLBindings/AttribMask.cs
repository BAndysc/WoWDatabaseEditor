using System;

namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    [Flags]
    public enum AttribMask
    {
        /// <summary>
        /// Original was GL_DEPTH_BUFFER_BIT = 0x00000100
        /// </summary>
        DepthBufferBit = 0x100,
        /// <summary>
        /// Original was GL_STENCIL_BUFFER_BIT = 0x00000400
        /// </summary>
        StencilBufferBit = 0x400,
        /// <summary>
        /// Original was GL_COLOR_BUFFER_BIT = 0x00004000
        /// </summary>
        ColorBufferBit = 0x4000,
        /// <summary>
        /// Original was GL_MULTISAMPLE_BIT = 0x20000000
        /// </summary>
        MultisampleBit = 0x20000000,
        /// <summary>
        /// Original was GL_MULTISAMPLE_BIT_3DFX = 0x20000000
        /// </summary>
        MultisampleBit3Dfx = 0x20000000,
        /// <summary>
        /// Original was GL_MULTISAMPLE_BIT_ARB = 0x20000000
        /// </summary>
        MultisampleBitArb = 0x20000000,
        /// <summary>
        /// Original was GL_MULTISAMPLE_BIT_EXT = 0x20000000
        /// </summary>
        MultisampleBitExt = 0x20000000
    }
}