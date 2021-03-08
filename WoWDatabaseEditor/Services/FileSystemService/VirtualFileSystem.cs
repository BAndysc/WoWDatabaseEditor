using System;
using System.Collections.Generic;
using System.IO;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.FileSystemService
{
    [AutoRegister]
    [SingleInstance]
    public class VirtualFileSystem : IVirtualFileSystem
    {      
        private Dictionary<string, string> mountedDirectories = new();

        public void MountDirectory(string virtualDirectory, string physicalDirectory)
        {
            if (string.IsNullOrWhiteSpace(virtualDirectory))
                throw new ArgumentException(nameof(virtualDirectory) + " cannot be null nor empty");
            
            mountedDirectories[virtualDirectory] = physicalDirectory;
        }

        public FileInfo ResolvePath(string virtualPath)
        {
            foreach (var mounted in mountedDirectories)
                if (virtualPath.StartsWith(mounted.Key))
                    return new FileInfo(Path.Join(mounted.Value, virtualPath.Substring(mounted.Key.Length)));

            return new FileInfo(virtualPath);
        }
    }
}