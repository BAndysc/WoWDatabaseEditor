using System.IO;

namespace WoWDatabaseEditorCore.Services.FileSystemService
{
    public interface IVirtualFileSystem
    {
        void MountDirectory(string virtualDirectory, string physicalDirectory);
        FileInfo ResolvePath(string virtualPath);
    }
}