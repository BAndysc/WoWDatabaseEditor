using System;
using System.IO;
using System.Linq;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration
{
    public interface ISourceSqlUpdateService
    {
        public void SaveAuthUpdate(string name, string content);
        public void SaveWorldUpdate(string name, string content);
    }

    [AutoRegister]
    [SingleInstance]
    public class SourceSqlUpdateService : ISourceSqlUpdateService
    {
        private readonly string PathUpdateAuth = "sql/updates/auth";
        private readonly string PathUpdateWorld = "sql/updates/world";
        private readonly ICoreSourceProvider sourceProvider;

        public SourceSqlUpdateService(ICoreSourceProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider;
        }

        private bool SupportsUpdates()
        {
            return AuthUpdatePath() != null &&
                   WorldUpdatePath() != null;
        }

        private string? GetFirstFolderIn(string path)
        {
            var first = sourceProvider.GetDirectoriesInDirectory(path).FirstOrDefault();
            if (first == null)
                return null;
            return Path.GetFileName(first);
        }

        private string? AuthUpdatePath()
        {
            var folder = GetFirstFolderIn(PathUpdateAuth);
            return folder == null ? null : Path.Join(PathUpdateAuth, folder);
        }
        
        private string? WorldUpdatePath()
        {
            var folder = GetFirstFolderIn(PathUpdateWorld);
            return folder == null ? null : Path.Join(PathUpdateWorld, folder);
        }

        private string GenerateUpdateFileName(string path, string name)
        {
            var now = DateTime.Now.ToString("yyyy_MM_dd");
            var todayUpdates = sourceProvider
                .GetFilesInDirectory(path, "sql")
                .Select(p => Path.GetFileNameWithoutExtension(p))
                .Where(p => p.StartsWith(now))
                .ToList();

            int id = 0;
            while (todayUpdates.Any(p => p.StartsWith(now + "_" + id.ToString().PadLeft(2, '0')))) id++;

            return Path.Join(path, $"{now}_{id:00}_{name}.sql");
        }
        
        public void SaveAuthUpdate(string name, string content)
        {
            if (!SupportsUpdates())
                return;

            sourceProvider.SaveFile(GenerateUpdateFileName(AuthUpdatePath()!, name), content);
        }

        public void SaveWorldUpdate(string name, string content)
        {
            if (!SupportsUpdates())
                return;
            
            sourceProvider.SaveFile(GenerateUpdateFileName(WorldUpdatePath()!, name), content);
        }
    }
}