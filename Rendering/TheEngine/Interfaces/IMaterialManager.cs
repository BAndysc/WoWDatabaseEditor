using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Resources;

namespace TheEngine.Interfaces
{
    public interface IMaterialManager
    {
        Material CreateMaterial(Pipeline pipeline);
        Material<T> CreateMaterial<T>(Pipeline pipeline) where T : unmanaged;
        Material GetMaterialByHandle(MaterialHandle handle);
    }
}
