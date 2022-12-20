using System.IO;
using System.Threading.Tasks;

namespace WDE.Profiles.Services;

public interface IProfilesFileSystem
{
    FileStream OpenFile(string relativePath, FileMode mode, FileAccess fileAccess);
    bool Exists(string relativePath);
    FileInfo GetFilePath(string relativePath);
    FileInfo GetProfileFilePath(string profile, string relativePath);
    DirectoryInfo GetProfileDirectoryPath(string profile);
    DirectoryInfo GetDirectoryPath(string relativePath);
    Task<string> ReadAllText(string relativePath);
}