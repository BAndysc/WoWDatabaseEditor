using System;
using System.IO;
using System.Runtime.InteropServices;
using WDE.Common.Profiles;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.Profiles.Services;

namespace WoWDatabaseEditorCore.Services.FileSystemService
{
    [AutoRegister]
    [SingleInstance]
    public class FileSystem : IFileSystem
    {
        private readonly IVirtualFileSystem vfs;
        public static readonly string APPLICATION_FOLDER = "WoWDatabaseEditor";
        
        public FileSystem(IVirtualFileSystem vfs)
        {
            this.vfs = vfs;
            var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            vfs.MountDirectory("~", Path.Join(localDataPath, APPLICATION_FOLDER, ProfileService.ReadDefaultProfileKey()));
            vfs.MountDirectory("/common", Path.Join(localDataPath, APPLICATION_FOLDER, "common"));
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

        public FileInfo ResolvePhysicalPath(string path)
        {
            return vfs.ResolvePath(path);
        }
    }
}