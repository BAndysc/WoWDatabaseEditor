using System;

namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    [Flags]
    public enum MemoryBarrierMask
    {
        /// <summary>
        /// Original was GL_VERTEX_ATTRIB_ARRAY_BARRIER_BIT = 0x00000001
        /// </summary>
        VertexAttribArrayBarrierBit = 0x1,
        /// <summary>
        /// Original was GL_VERTEX_ATTRIB_ARRAY_BARRIER_BIT_EXT = 0x00000001
        /// </summary>
        VertexAttribArrayBarrierBitExt = 0x1,
        /// <summary>
        /// Original was GL_ELEMENT_ARRAY_BARRIER_BIT = 0x00000002
        /// </summary>
        ElementArrayBarrierBit = 0x2,
        /// <summary>
        /// Original was GL_ELEMENT_ARRAY_BARRIER_BIT_EXT = 0x00000002
        /// </summary>
        ElementArrayBarrierBitExt = 0x2,
        /// <summary>
        /// Original was GL_UNIFORM_BARRIER_BIT = 0x00000004
        /// </summary>
        UniformBarrierBit = 0x4,
        /// <summary>
        /// Original was GL_UNIFORM_BARRIER_BIT_EXT = 0x00000004
        /// </summary>
        UniformBarrierBitExt = 0x4,
        /// <summary>
        /// Original was GL_TEXTURE_FETCH_BARRIER_BIT = 0x00000008
        /// </summary>
        TextureFetchBarrierBit = 0x8,
        /// <summary>
        /// Original was GL_TEXTURE_FETCH_BARRIER_BIT_EXT = 0x00000008
        /// </summary>
        TextureFetchBarrierBitExt = 0x8,
        /// <summary>
        /// Original was GL_SHADER_GLOBAL_ACCESS_BARRIER_BIT_NV = 0x00000010
        /// </summary>
        ShaderGlobalAccessBarrierBitNv = 0x10,
        /// <summary>
        /// Original was GL_SHADER_IMAGE_ACCESS_BARRIER_BIT = 0x00000020
        /// </summary>
        ShaderImageAccessBarrierBit = 0x20,
        /// <summary>
        /// Original was GL_SHADER_IMAGE_ACCESS_BARRIER_BIT_EXT = 0x00000020
        /// </summary>
        ShaderImageAccessBarrierBitExt = 0x20,
        /// <summary>
        /// Original was GL_COMMAND_BARRIER_BIT = 0x00000040
        /// </summary>
        CommandBarrierBit = 0x40,
        /// <summary>
        /// Original was GL_COMMAND_BARRIER_BIT_EXT = 0x00000040
        /// </summary>
        CommandBarrierBitExt = 0x40,
        /// <summary>
        /// Original was GL_PIXEL_BUFFER_BARRIER_BIT = 0x00000080
        /// </summary>
        PixelBufferBarrierBit = 0x80,
        /// <summary>
        /// Original was GL_PIXEL_BUFFER_BARRIER_BIT_EXT = 0x00000080
        /// </summary>
        PixelBufferBarrierBitExt = 0x80,
        /// <summary>
        /// Original was GL_TEXTURE_UPDATE_BARRIER_BIT = 0x00000100
        /// </summary>
        TextureUpdateBarrierBit = 0x100,
        /// <summary>
        /// Original was GL_TEXTURE_UPDATE_BARRIER_BIT_EXT = 0x00000100
        /// </summary>
        TextureUpdateBarrierBitExt = 0x100,
        /// <summary>
        /// Original was GL_BUFFER_UPDATE_BARRIER_BIT = 0x00000200
        /// </summary>
        BufferUpdateBarrierBit = 0x200,
        /// <summary>
        /// Original was GL_BUFFER_UPDATE_BARRIER_BIT_EXT = 0x00000200
        /// </summary>
        BufferUpdateBarrierBitExt = 0x200,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_BARRIER_BIT = 0x00000400
        /// </summary>
        FramebufferBarrierBit = 0x400,
        /// <summary>
        /// Original was GL_FRAMEBUFFER_BARRIER_BIT_EXT = 0x00000400
        /// </summary>
        FramebufferBarrierBitExt = 0x400,
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_BARRIER_BIT = 0x00000800
        /// </summary>
        TransformFeedbackBarrierBit = 0x800,
        /// <summary>
        /// Original was GL_TRANSFORM_FEEDBACK_BARRIER_BIT_EXT = 0x00000800
        /// </summary>
        TransformFeedbackBarrierBitExt = 0x800,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BARRIER_BIT = 0x00001000
        /// </summary>
        AtomicCounterBarrierBit = 0x1000,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BARRIER_BIT_EXT = 0x00001000
        /// </summary>
        AtomicCounterBarrierBitExt = 0x1000,
        /// <summary>
        /// Original was GL_SHADER_STORAGE_BARRIER_BIT = 0x00002000
        /// </summary>
        ShaderStorageBarrierBit = 0x2000,
        /// <summary>
        /// Original was GL_CLIENT_MAPPED_BUFFER_BARRIER_BIT = 0x00004000
        /// </summary>
        ClientMappedBufferBarrierBit = 0x4000,
        /// <summary>
        /// Original was GL_CLIENT_MAPPED_BUFFER_BARRIER_BIT_EXT = 0x00004000
        /// </summary>
        ClientMappedBufferBarrierBitExt = 0x4000,
        /// <summary>
        /// Original was GL_QUERY_BUFFER_BARRIER_BIT = 0x00008000
        /// </summary>
        QueryBufferBarrierBit = 0x8000,
        /// <summary>
        /// Original was GL_ALL_BARRIER_BITS = 0xFFFFFFFF
        /// </summary>
        AllBarrierBits = -1,
        /// <summary>
        /// Original was GL_ALL_BARRIER_BITS_EXT = 0xFFFFFFFF
        /// </summary>
        AllBarrierBitsExt = -1
    }
}