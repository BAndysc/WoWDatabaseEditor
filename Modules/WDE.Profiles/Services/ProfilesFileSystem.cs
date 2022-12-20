using System;
using System.IO;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Profiles.Services;

[AutoRegister]
[SingleInstance]
public class ProfilesFileSystem : IProfilesFileSystem
{
    private string rootPath;
    private string rootCommonPath;

    public ProfilesFileSystem()
    {
        var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        rootPath = Path.Join(localDataPath, "WoWDatabaseEditor");
        rootCommonPath = Path.Join(rootPath, "common");
        
        if (!Directory.Exists(rootCommonPath))
            Directory.CreateDirectory(rootCommonPath);
    }

    public FileStream OpenFile(string relativePath, FileMode mode, FileAccess fileAccess)
    {
        var path = Path.Join(rootCommonPath, relativePath);
        return File.Open(path, mode, fileAccess);
    }

    public bool Exists(string relativePath)
    {
        var path = Path.Join(rootCommonPath, relativePath);
        return File.Exists(path);
    }

    public FileInfo GetFilePath(string relativePath)
    {
        var path = Path.Join(rootCommonPath, relativePath);
        return new FileInfo(path);
    }

    public FileInfo GetProfileFilePath(string profile, string relativePath)
    {
        var path = Path.Join(rootPath, profile, relativePath);
        return new FileInfo(path);
    }

    public DirectoryInfo GetProfileDirectoryPath(string profile)
    {
        var path = Path.Join(rootPath, profile);
        return new DirectoryInfo(path);
    }

    public DirectoryInfo GetDirectoryPath(string relativePath)
    {
        var path = Path.Join(rootCommonPath, relativePath);
        return new DirectoryInfo(path);
    }

    public Task<string> ReadAllText(string relativePath)
    {
        var path = Path.Join(rootCommonPath, relativePath);
        return File.ReadAllTextAsync(path);
    }
}