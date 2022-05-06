using System;
using System.Collections.Generic;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;

namespace TheEngine.Managers
{
    internal class MaterialManager : IMaterialManager, IDisposable
    {
        private Engine engine;
        private List<Material> materials = new();

        public MaterialManager(Engine engine)
        {
            this.engine = engine;
        }

        public Material CreateMaterial(ShaderHandle shader, ShaderHandle? instancedShader)
        {
            var m = new Material(engine, shader, instancedShader, new MaterialHandle(materials.Count));
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
