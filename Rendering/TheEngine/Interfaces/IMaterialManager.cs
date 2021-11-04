using TheEngine.Entities;
using TheEngine.Handles;

namespace TheEngine.Interfaces
{
    public interface IMaterialManager
    {
        Material CreateMaterial(ShaderHandle shader);
        Material CreateMaterial(string shaderPath);
    }
}
