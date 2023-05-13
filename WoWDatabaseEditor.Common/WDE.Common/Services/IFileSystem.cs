using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WDE.Common.Services
{
    public interface IFileSystem
    {
        bool Exists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string text);

        Stream OpenRead(string path);
        Stream OpenWrite(string path);
        FileInfo ResolvePhysicalPath(string path);
    }

    public static class FileSystemExtensions
    {
        public static string[] ReadAllLines(this IFileSystem fs, string virtualPath)
        {
            return File.ReadAllLines(fs.ResolvePhysicalPath(virtualPath).FullName);
        }
        
        public static IEnumerable<string> ReadLines(this IFileSystem fs, string virtualPath)
        {
            return File.ReadLines(fs.ResolvePhysicalPath(virtualPath).FullName);
        }
        
        public static Task<string[]> ReadAllLinesAsync(this IFileSystem fs, string virtualPath)
        {
            return File.ReadAllLinesAsync(fs.ResolvePhysicalPath(virtualPath).FullName);
        }
        public static DateTime GetLastWriteTime(this IFileSystem fs, string virtualPath)
        {
            return File.GetLastWriteTime(fs.ResolvePhysicalPath(virtualPath).FullName);
        }
        public static byte[] ReadAllBytes(this IFileSystem fs, string virtualPath)
        {
            return File.ReadAllBytes(fs.ResolvePhysicalPath(virtualPath).FullName);
        }
        public static void WriteAllBytes(this IFileSystem fs, string virtualPath, byte[] bytes)
        {
            File.WriteAllBytes(fs.ResolvePhysicalPath(virtualPath).FullName, bytes);
        }
        
        public static string[] GetFiles(this IFileSystem fs, string virtualPath)
        {
            return Directory.GetFiles(fs.ResolvePhysicalPath(virtualPath).FullName);
        }
    }
}