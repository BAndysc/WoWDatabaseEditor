using System.Diagnostics;

namespace TheEngine.Handles
{
    /// <summary>
    /// The precomputed 64-bit draw-sort/batching key stored on every <see cref="TheEngine.Components.MeshRenderer"/>
    /// and consumed by the ObjectDrawRenderStage radix sort. A plain ascending sort of <see cref="Value"/>
    /// yields the batchable draw order.
    ///
    /// Bit layout (MSB -> LSB), highest priority first:
    /// <code>
    ///   [63]     opaque(0)/transparent(1)    1 bit
    ///   [55-62]  shader id                   8 bits  -> 256
    ///   [46-54]  pipeline id                 9 bits  -> 512
    ///   [12-45]  mesh handle                34 bits  -> 17,179,869,184
    ///   [0-11]   submesh id                 12 bits  -> 4096
    /// </code>
    /// Material is NOT in the sort key: with fully-bindless textures, same pipeline guarantees
    /// compatible descriptor sets, so renderers sharing (pipeline, mesh, submesh) can be batched
    /// regardless of which Material object they hold.
    ///
    /// The Max* constants below are the single source of truth for these limits; the managers that mint
    /// shader/pipeline/mesh/submesh ids throw when they would exceed them.
    /// </summary>
    public readonly struct SortKey : IComparable<SortKey>, IEquatable<SortKey>
    {
        private const int ShaderBits = 8;
        private const int PipelineBits = 9;
        private const int MeshBits = 34;
        private const int SubMeshBits = 12;

        /// <summary>Max distinct shader ids representable in the key (8 bits). Note: ShaderManager enforces a tighter 255.</summary>
        public const int MaxShaders = 1 << ShaderBits;      // 256
        /// <summary>Max pipelines: the pipeline id occupies 9 bits.</summary>
        public const int MaxPipelines = 1 << PipelineBits;  // 512
        /// <summary>Max meshes: the mesh handle occupies 34 bits.</summary>
        public const long MaxMeshes = 1L << MeshBits;       // 17,179,869,184
        /// <summary>Max submeshes per mesh: the submesh id occupies 12 bits.</summary>
        public const int MaxSubMeshes = 1 << SubMeshBits;   // 4096

        private const int SubMeshShift = 0;
        private const int MeshShift = 12;
        private const int PipelineShift = 46;
        private const int ShaderShift = 55;
        private const int OpaqueShift = 63;

        private const int ShaderMask = MaxShaders - 1;
        private const int PipelineMask = MaxPipelines - 1;
        private const long MeshMask = MaxMeshes - 1;
        private const int SubMeshMask = MaxSubMeshes - 1;

        /// <summary>The packed 64-bit key. Sorting renderers by this value ascending produces the draw order.</summary>
        public readonly ulong Value;

        public SortKey(ulong value) => Value = value;

        /// <summary>Packs the field ids into the 64-bit key. A negative <paramref name="pipelineId"/> (an empty
        /// pipeline handle) is simply masked into the field; such renderers are degenerate and not drawn.</summary>
        public static SortKey Build(bool opaque, int shaderId, int pipelineId, int meshId, int subMeshId)
        {
            Debug.Assert(shaderId < MaxShaders, "shader id exceeds 8 bits");
            Debug.Assert(pipelineId < MaxPipelines, "pipeline id exceeds 9 bits (max 512 pipelines)");
            Debug.Assert((long)meshId < MaxMeshes, "mesh handle exceeds 34 bits");
            Debug.Assert(subMeshId >= 0 && subMeshId < MaxSubMeshes, "submesh id exceeds 12 bits (max 4096 submeshes)");

            ulong transparentBit = opaque ? 0UL : 1UL;
            ulong value = (transparentBit << OpaqueShift)
                          | ((ulong)(shaderId & ShaderMask) << ShaderShift)
                          | ((ulong)(pipelineId & PipelineMask) << PipelineShift)
                          | ((ulong)((long)meshId & MeshMask) << MeshShift)
                          | ((ulong)(subMeshId & SubMeshMask) << SubMeshShift);
            return new SortKey(value);
        }

        public static implicit operator ulong(SortKey key) => key.Value;

        public int CompareTo(SortKey other) => Value.CompareTo(other.Value);
        public bool Equals(SortKey other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is SortKey other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"SortKey(0x{Value:X16})";
    }
}
