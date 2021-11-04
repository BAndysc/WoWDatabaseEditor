namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.MemoryBarrierByRegion
    /// </summary>
    public enum MemoryBarrierRegionFlags
    {
        /// <summary>
        /// Original was GL_GL_UNIFORM_BARRIER_BIT = 0x00000004
        /// </summary>
        GlUniformBarrierBit = 4,
        /// <summary>
        /// Original was GL_GL_TEXTURE_FETCH_BARRIER_BIT = 0x00000008
        /// </summary>
        GlTextureFetchBarrierBit = 8,
        /// <summary>
        /// Original was GL_GL_SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020
        /// </summary>
        GlShaderImageAccessBarrierBit = 0x20,
        /// <summary>
        /// Original was GL_GL_FRAMEBUFFER_BARRIER_BIT = 0x00000400
        /// </summary>
        GlFramebufferBarrierBit = 0x400,
        /// <summary>
        /// Original was GL_GL_ATOMIC_COUNTER_BARRIER_BIT = 0x00001000
        /// </summary>
        GlAtomicCounterBarrierBit = 0x1000,
        /// <summary>
        /// Original was GL_GL_ALL_BARRIER_BITS = 0xFFFFFFFF
        /// </summary>
        GlAllBarrierBits = -1
    }
}