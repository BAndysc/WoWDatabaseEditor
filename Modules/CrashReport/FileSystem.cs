using System;
using System.IO;
using WDE.Common.Services;

namespace CrashReport;

public class FileSystem : IFileSystem
{
    public bool Exists(string path) => ResolvePhysicalPath(path).Exists;
    public string ReadAllText(string path) => File.ReadAllText(ResolvePhysicalPath(path).FullName);
    public void WriteAllText(string path, string text) => File.WriteAllText(ResolvePhysicalPath(path).FullName, text);
    public Stream OpenRead(string path) => File.OpenRead(ResolvePhysicalPath(path).FullName);
    public Stream OpenWrite(string path) => File.OpenWrite(ResolvePhysicalPath(path).FullName);
    public FileInfo ResolvePhysicalPath(string path)
    {
        if (path.StartsWith("~"))
        {
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Join(localDataPath, "WoWDatabaseEditor");
            return new FileInfo(Path.Join(dir, path.Substring(1)));
        }
        return new FileInfo(path);
    }
}