using TheEngine.Handles;

namespace TheEngine.Interfaces
{
    public interface IShaderManager
    {
        ShaderHandle LoadShader(string path, bool instancing);
    }
}
