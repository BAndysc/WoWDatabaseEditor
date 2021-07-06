using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration
{
    [UniqueProvider]
    public interface ICoreSourceProvider
    {
        string? ReadFile(string path);
        IEnumerable<string> ReadLines(string path);
        IEnumerable<string> GetFilesInDirectory(string directory, string extension);
        IEnumerable<string> GetDirectoriesInDirectory(string directory);
        bool SaveFile(string path, string file);
        bool DirectoryExists(string path);
        bool FileExists(string path);
    }
}