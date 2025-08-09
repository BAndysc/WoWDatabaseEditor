using System;
using System.Collections.Generic;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;

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
        
        public Material CreateMaterial(ShaderHandle shaderHandle, ShaderHandle? instancedShader)
        {
            var m = new Material(engine, shaderHandle, instancedShader, new MaterialHandle(materials.Count));
            var shader = engine.shaderManager.GetShaderByHandle(shaderHandle);

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

        public Material<T> CreateMaterial<T>(ShaderHandle shaderHandle, ShaderHandle? instancedShader) where T : unmanaged
        {
            var m = new Material<T>(engine, shaderHandle, instancedShader, new MaterialHandle(materials.Count));
            var shader = engine.shaderManager.GetShaderByHandle(shaderHandle);

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

        public Material CreateMaterial(string shaderPath)
        {
            var shader = engine.ShaderManager.LoadShader(shaderPath, false);
            ShaderHandle? instanced = engine.ShaderManager.LoadShader(shaderPath, true);
            if (!engine.shaderManager.GetShaderByHandle(instanced.Value).Instancing)
                instanced = null;
            return CreateMaterial(shader, instanced);
        }

        public Material<T> CreateMaterial<T>(string shaderPath) where T : unmanaged
        {
            var shader = engine.ShaderManager.LoadShader(shaderPath, false);
            ShaderHandle? instanced = engine.ShaderManager.LoadShader(shaderPath, true);
            if (!engine.shaderManager.GetShaderByHandle(instanced.Value).Instancing)
                instanced = null;
            return CreateMaterial<T>(shader, instanced);
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
