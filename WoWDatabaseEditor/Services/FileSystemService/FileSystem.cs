using System;
using System.IO;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.FileSystemService
{
    [AutoRegister]
    [SingleInstance]
    public class FileSystem : IFileSystem
    {
        private readonly IVirtualFileSystem vfs;
        private static readonly string APPLICATION_FOLDER = "WoWDatabaseEditor";
        
        public FileSystem(IVirtualFileSystem vfs)
        {
            this.vfs = vfs;
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            vfs.MountDirectory("~", Path.Join(localDataPath, APPLICATION_FOLDER, TryGetProfile()));
        }

        private string TryGetProfile()
        {
            if (File.Exists("profile"))
                return File.ReadAllText("profile");
            return "default";
        }

        public bool Exists(string virtualPath)
        {
            return vfs.ResolvePath(virtualPath).Exists;
        }

        public string ReadAllText(string virtualPath)
        {
            var path = vfs.ResolvePath(virtualPath);
            return File.ReadAllText(path.FullName);
        }

        public void WriteAllText(string virtualPath, string text)
        {
            using var writer = OpenWrite(virtualPath);
            using var stream = new StreamWriter(writer);
            stream.Write(text);
        }

        public Stream OpenRead(string virtualPath)
        {
            return vfs.ResolvePath(virtualPath).OpenRead();
        }

        public Stream OpenWrite(string virtualPath)
        {
            var path = vfs.ResolvePath(virtualPath);
            path.Directory?.Create();
            return path.Open(FileMode.Create, FileAccess.Write);
        }
    }
}