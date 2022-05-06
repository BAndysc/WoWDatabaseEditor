using TheEngine.Entities;
using TheEngine.Handles;

namespace TheEngine.Interfaces
{
    public interface IMaterialManager
    {
        Material CreateMaterial(ShaderHandle shader, ShaderHandle? instancedShader);
        Material CreateMaterial(string shaderPath);
        Material GetMaterialByHandle(MaterialHandle handle);
    }
}
