using System.Runtime.CompilerServices;
using TheEngine.Resources;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;

namespace TheEngine.Managers
{
    /// <summary>Per-material-struct-type persistent SSBO: one row per Material&lt;T&gt; instance, indexed by
    /// Material.MaterialArrayIndex and re-packed/uploaded once per frame by MaterialManager.Update().</summary>
    internal class MaterialTypeArray
    {
        // slot-addressed: a disposed material's row is nulled and recycled via freeSlots, so
        // per-instance material clones (created/destroyed with world objects) don't grow the
        // SSBO (and the per-frame repack) forever.
        private readonly List<Material?> materials = new();
        private readonly Stack<int> freeSlots = new();
        private readonly int stride;
        private readonly INativeBuffer<byte> buffer;
        private byte[] scratch = Array.Empty<byte>();

        public INativeBuffer Buffer => buffer;

        public MaterialTypeArray(Engine engine, int stride)
        {
            this.stride = stride;
            buffer = engine.CreateBuffer<byte>(BufferTypeEnum.StructuredBuffer, Math.Max(stride, 16));
        }

        public int Add(Material material)
        {
            if (freeSlots.TryPop(out var freeIndex))
            {
                materials[freeIndex] = material;
                return freeIndex;
            }
            materials.Add(material);
            return materials.Count - 1;
        }

        public void Remove(Material material)
        {
            int index = material.MaterialArrayIndex;
            if (index < 0 || index >= materials.Count || !ReferenceEquals(materials[index], material))
                return;
            materials[index] = null;
            freeSlots.Push(index);
        }

        public void Repack()
        {
            int totalBytes = materials.Count * stride;
            if (totalBytes == 0)
                return;

            if (scratch.Length != totalBytes)
                scratch = new byte[totalBytes];

            for (int i = 0; i < materials.Count; i++)
            {
                var row = scratch.AsSpan(i * stride, stride);
                if (materials[i] is { } material)
                    material.MaterialDataBytes.CopyTo(row);
                else
                    row.Clear();
            }

            buffer.UpdateBuffer(scratch);
        }
    }

    internal class MaterialManager : IMaterialManager, IDisposable
    {
        private Engine engine;
        private List<WeakReference<Material>> materials = new();
        private Dictionary<Type, MaterialTypeArray> typeArrays = new();

        public MaterialManager(Engine engine)
        {
            this.engine = engine;
        }

        public Material CreateMaterial(Pipeline pipeline)
        {
            var m = new Material(engine, pipeline, new MaterialHandle(materials.Count));
            materials.Add(new WeakReference<Material>(m));
            return m;
        }

        public Material<T> CreateMaterial<T>(Pipeline pipeline) where T : unmanaged
        {
            var m = new Material<T>(engine, pipeline, new MaterialHandle(materials.Count));

            if (!typeArrays.TryGetValue(m.GetType(), out var typeArray))
                typeArrays[m.GetType()] = typeArray = new MaterialTypeArray(engine, Unsafe.SizeOf<T>());
            m.MaterialArrayIndex = typeArray.Add(m);
            // bound directly at set 1 binding 0 (see VulkanCommandList.BindMaterialResources); no-op for
            // shaders that don't declare a MaterialDataArray SSBO (nothing resolves binding 0 then).
            // Lives on the pipeline (shared by all materials of this type); a pipeline never carries two
            // different material struct types, so this is idempotent.
            System.Diagnostics.Debug.Assert(m.Pipeline.MaterialArrayBuffer == null || ReferenceEquals(m.Pipeline.MaterialArrayBuffer, typeArray.Buffer),
                "one pipeline used with two different material struct types");
            m.Pipeline.MaterialArrayBuffer = typeArray.Buffer;

            materials.Add(new WeakReference<Material>(m));
            return m;
        }

        public Material<T> CloneMaterial<T>(Material<T> source) where T : unmanaged
        {
            var clone = CreateMaterial<T>(source.Pipeline);
            var data = source.MaterialData;
            clone.SetMaterialData(ref data);
            clone.CopyKeepAlivesFrom(source);
            return clone;
        }

        public void DisposeMaterial(Material material)
        {
            if (material.MaterialArrayIndex >= 0 && typeArrays.TryGetValue(material.GetType(), out var typeArray))
                typeArray.Remove(material);
            material.MaterialArrayIndex = -1;
        }

        /// <summary>Re-packs and re-uploads every per-type MaterialData SSBO from each material's current bytes.
        /// Called once per frame; no-op on backends without bindless support (GL doesn't use these SSBOs).</summary>
        public void Update()
        {
            foreach (var typeArray in typeArrays.Values)
                typeArray.Repack();
        }

        /// <summary>The persistent per-type MaterialData SSBO that <paramref name="material"/>'s row lives in, or null
        /// if the material's struct type has no registered array (e.g. bindless not supported).</summary>
        public INativeBuffer? GetTypeBuffer(Material material)
        {
            return typeArrays.TryGetValue(material.GetType(), out var typeArray) ? typeArray.Buffer : null;
        }

        public void Dispose()
        {
            materials.Clear();
            materials = null!;
        }

        public Material GetMaterialByHandle(MaterialHandle handle)
        {
            if (materials[handle.Handle].TryGetTarget(out var material))
            {
                return material;
            }
            return null;
        }

    }
}
