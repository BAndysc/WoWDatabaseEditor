using System.IO;

namespace WDE.Common.Services
{
    public interface IFileSystem
    {
        bool Exists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string text);

        Stream OpenRead(string path);
        Stream OpenWrite(string path);
    }
}