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

        public Material CreateMaterial(ShaderHandle shader)
        {
            var m = new Material(engine, shader, new MaterialHandle(materials.Count));
            materials.Add(m);
            return m;
        }

        public Material CreateMaterial(string shaderPath)
        {
            return CreateMaterial(engine.ShaderManager.LoadShader(shaderPath));
        }

        public void Dispose()
        {
            
        }

        public Material GetMaterialByHandle(MaterialHandle handle)
        {
            return materials[handle.Handle];
        }
    }
}
