using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Resources;

namespace TheEngine.Interfaces
{
    public interface IMaterialManager
    {
        Material CreateMaterial(Pipeline pipeline);
        Material<T> CreateMaterial<T>(Pipeline pipeline) where T : unmanaged;
        /// <summary>A new material with the same pipeline, constant data and texture keep-alives as
        /// <paramref name="source"/>. Use to give an object instance its own copy of a shared
        /// (per-model cached) material so per-instance state doesn't bleed onto other instances.
        /// Pair with <see cref="DisposeMaterial"/> or its SSBO row leaks (repacked every frame).</summary>
        Material<T> CloneMaterial<T>(Material<T> source) where T : unmanaged;
        /// <summary>Frees the material's row in the per-type MaterialData SSBO for reuse. The material
        /// must not be referenced by any renderer afterwards.</summary>
        void DisposeMaterial(Material material);
        Material GetMaterialByHandle(MaterialHandle handle);
    }
}
