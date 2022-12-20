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
        private List<Material> materials = new();
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
                if (uniform.Value == ShaderVariableType.Int)
                    m.SetUniformInt(uniform.Key, 0);
                else if (uniform.Value == ShaderVariableType.Float)
                    m.SetUniform(uniform.Key, 0);
                else if (uniform.Value == ShaderVariableType.Float2)
                    throw new Exception("Float2 not supported");
                else if (uniform.Value == ShaderVariableType.Float3)
                    m.SetUniform(uniform.Key, Vector3.Zero);
                else if (uniform.Value == ShaderVariableType.Float4)
                    m.SetUniform(uniform.Key, Vector4.Zero);
                else if (uniform.Value == ShaderVariableType.Matrix)
                    m.SetUniform(uniform.Key, Matrix.Identity);
                else if (uniform.Value == ShaderVariableType.Sampler2D)
                    m.SetTexture(uniform.Key, engine.textureManager.EmptyTexture);
                else if (uniform.Value == ShaderVariableType.SamplerBuffer)
                    m.SetBuffer(uniform.Key, smallEmptyBuffer);
            }
            
            materials.Add(m);
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

        public void Dispose()
        {
            smallEmptyBuffer.Dispose();
            materials.Clear();
            materials = null!;
        }

        public Material GetMaterialByHandle(MaterialHandle handle)
        {
            return materials[handle.Handle];
        }

        public void InvalidateShaderCache()
        {
            foreach (var material in materials)
            {
                material.InvalidateShaderCache();
            }
        }
    }
}
