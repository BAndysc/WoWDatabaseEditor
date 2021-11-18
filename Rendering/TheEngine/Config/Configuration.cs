using TheEngine.Interfaces;

namespace TheEngine.Config
{
    public class Configuration : IConfiguration
    {
        public string ShaderDirectory { get; private set;  }

        public void SetShaderDirectory(string path)
        {
            ShaderDirectory = path;
        }
    }
}
