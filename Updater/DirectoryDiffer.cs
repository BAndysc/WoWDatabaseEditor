using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Updater
{
    public class DirectoryDiffer
    {
        public List<FileSystemInfo> GenerateDiff(DirectoryInfo left, DirectoryInfo right)
        {
            var list = new List<FileSystemInfo>();
            AppendDiff(list, left, right);
            return list;
        }

        private void AppendDiff(List<FileSystemInfo> list, DirectoryInfo left, DirectoryInfo right)
        {
            var rightDirectories = right.EnumerateDirectories().ToDictionary(d => d.Name, d => d);
            var rightFiles = right.EnumerateFiles().ToDictionary(d => d.Name, d => d);

            foreach (var dir in left.EnumerateDirectories())
            {
                if (rightDirectories.TryGetValue(dir.Name, out var rightDir))
                    AppendDiff(list, dir, rightDir);
                else
                    list.Add(dir);
            }
            
            foreach (var file in left.EnumerateFiles())
            {
                if (!rightFiles.TryGetValue(file.Name, out var rightDir))
                    list.Add(file);
            }
        }
    }
}