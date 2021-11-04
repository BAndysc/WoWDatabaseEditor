namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.BufferStorage, GL.NamedBufferStorage and 1 other function
    /// </summary>
    public enum BufferStorageFlags
    {
        /// <summary>
        /// Original was GL_NONE = 0
        /// </summary>
        None = 0,
        /// <summary>
        /// Original was GL_MAP_READ_BIT = 0x0001
        /// </summary>
        MapReadBit = 1,
        /// <summary>
        /// Original was GL_MAP_WRITE_BIT = 0x0002
        /// </summary>
        MapWriteBit = 2,
        /// <summary>
        /// Original was GL_MAP_PERSISTENT_BIT = 0x0040
        /// </summary>
        MapPersistentBit = 0x40,
        /// <summary>
        /// Original was GL_MAP_COHERENT_BIT = 0x0080
        /// </summary>
        MapCoherentBit = 0x80,
        /// <summary>
        /// Original was GL_DYNAMIC_STORAGE_BIT = 0x0100
        /// </summary>
        DynamicStorageBit = 0x100,
        /// <summary>
        /// Original was GL_CLIENT_STORAGE_BIT = 0x0200
        /// </summary>
        ClientStorageBit = 0x200
    }
}