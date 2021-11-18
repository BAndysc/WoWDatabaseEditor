namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.DebugMessageControl, GL.Arb.DebugMessageInsert and 6 other functions
    /// </summary>
    public enum DebugSource
    {
        /// <summary>
        /// Original was GL_DONT_CARE = 0x1100
        /// </summary>
        DontCare = 4352,
        /// <summary>
        /// Original was GL_DEBUG_SOURCE_API = 0x8246
        /// </summary>
        DebugSourceApi = 33350,
        /// <summary>
        /// Original was GL_DEBUG_SOURCE_WINDOW_SYSTEM = 0x8247
        /// </summary>
        DebugSourceWindowSystem = 33351,
        /// <summary>
        /// Original was GL_DEBUG_SOURCE_SHADER_COMPILER = 0x8248
        /// </summary>
        DebugSourceShaderCompiler = 33352,
        /// <summary>
        /// Original was GL_DEBUG_SOURCE_THIRD_PARTY = 0x8249
        /// </summary>
        DebugSourceThirdParty = 33353,
        /// <summary>
        /// Original was GL_DEBUG_SOURCE_APPLICATION = 0x824A
        /// </summary>
        DebugSourceApplication = 33354,
        /// <summary>
        /// Original was GL_DEBUG_SOURCE_OTHER = 0x824B
        /// </summary>
        DebugSourceOther = 33355
    }
}