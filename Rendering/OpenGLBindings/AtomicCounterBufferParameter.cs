namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.GetActiveAtomicCounterBuffer
    /// </summary>
    public enum AtomicCounterBufferParameter
    {
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_COMPUTE_SHADER = 0x90ED
        /// </summary>
        AtomicCounterBufferReferencedByComputeShader = 37101,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_BINDING = 0x92C1
        /// </summary>
        AtomicCounterBufferBinding = 37569,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_DATA_SIZE = 0x92C4
        /// </summary>
        AtomicCounterBufferDataSize = 37572,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTERS = 0x92C5
        /// </summary>
        AtomicCounterBufferActiveAtomicCounters = 37573,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_ACTIVE_ATOMIC_COUNTER_INDICES = 0x92C6
        /// </summary>
        AtomicCounterBufferActiveAtomicCounterIndices = 37574,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_VERTEX_SHADER = 0x92C7
        /// </summary>
        AtomicCounterBufferReferencedByVertexShader = 37575,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_CONTROL_SHADER = 0x92C8
        /// </summary>
        AtomicCounterBufferReferencedByTessControlShader = 37576,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_TESS_EVALUATION_SHADER = 0x92C9
        /// </summary>
        AtomicCounterBufferReferencedByTessEvaluationShader = 37577,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_GEOMETRY_SHADER = 0x92CA
        /// </summary>
        AtomicCounterBufferReferencedByGeometryShader = 37578,
        /// <summary>
        /// Original was GL_ATOMIC_COUNTER_BUFFER_REFERENCED_BY_FRAGMENT_SHADER = 0x92CB
        /// </summary>
        AtomicCounterBufferReferencedByFragmentShader = 37579
    }
}