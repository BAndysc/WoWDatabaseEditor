using TheAvaloniaOpenGL.Resources;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Resources;

namespace TheEngine.Managers
{
    internal class MaterialManager : IMaterialManager, IDisposable
    {
        private Engine engine;
        private List<WeakReference<Material>> materials = new();
        private NativeBuffer<Vector4> smallEmptyBuffer;

        public MaterialManager(Engine engine)
        {
            this.engine = engine;
            smallEmptyBuffer = engine.CreateBuffer<Vector4>(BufferTypeEnum.StructuredBuffer, 4, BufferInternalFormat.Float4);
        }
        
        public Material CreateMaterial(Pipeline pipeline)
        {
            var m = new Material(engine, pipeline, new MaterialHandle(materials.Count));
            var shader = pipeline.Shader;

            foreach (var uniform in shader.Uniforms)
            {
                if (uniform.Value == ShaderVariableType.Sampler2D)
                    m.SetTexture(uniform.Key, engine.textureManager.EmptyTexture);
                else if (uniform.Value == ShaderVariableType.SamplerBuffer)
                    m.SetBuffer(uniform.Key, smallEmptyBuffer);
            }
            
            materials.Add(new WeakReference<Material>(m));
            return m;
        }

        public Material<T> CreateMaterial<T>(Pipeline pipeline) where T : unmanaged
        {
            var m = new Material<T>(engine, pipeline, new MaterialHandle(materials.Count));
            var shader = pipeline.Shader;

            foreach (var uniform in shader.Uniforms)
            {
                if (uniform.Value == ShaderVariableType.Sampler2D)
                    m.SetTexture(uniform.Key, engine.textureManager.EmptyTexture);
                else if (uniform.Value == ShaderVariableType.SamplerBuffer)
                    m.SetBuffer(uniform.Key, smallEmptyBuffer);
            }

            materials.Add(new WeakReference<Material>(m));
            return m;
        }

        public void Dispose()
        {
            smallEmptyBuffer.Dispose();
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

        public void InvalidateShaderCache()
        {
            foreach (var material in materials)
            {
                if (material.TryGetTarget(out var target))
                    target.InvalidateShaderCache();
            }
        }
    }
}
