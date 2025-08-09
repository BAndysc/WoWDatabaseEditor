using TheEngine.Entities;
using TheEngine.Handles;

namespace TheEngine.Interfaces
{
    public interface IMaterialManager
    {
        Material CreateMaterial(ShaderHandle shader, ShaderHandle? instancedShader);
        Material CreateMaterial(string shaderPath);
        Material<T> CreateMaterial<T>(ShaderHandle shader, ShaderHandle? instancedShader) where T : unmanaged;
        Material<T> CreateMaterial<T>(string shaderPath) where T : unmanaged;
        Material GetMaterialByHandle(MaterialHandle handle);
    }
}
