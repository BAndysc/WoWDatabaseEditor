using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;

namespace TheEngine.Managers
{
    internal class MaterialManager : IMaterialManager, IDisposable
    {
        private Engine engine;

        public MaterialManager(Engine engine)
        {
            this.engine = engine;
        }

        public Material CreateMaterial(ShaderHandle shader)
        {
            return new Material(engine, shader);
        }

        public Material CreateMaterial(string shaderPath)
        {
            return CreateMaterial(engine.ShaderManager.LoadShader(shaderPath));
        }

        public void Dispose()
        {
            
        }
    }
}
