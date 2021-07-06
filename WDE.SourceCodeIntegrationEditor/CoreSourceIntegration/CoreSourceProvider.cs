using System.Collections.Generic;
using System.IO;
using System.Linq;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration
{
    [AutoRegister]
    [SingleInstance]
    public class CoreSourceProvider : ICoreSourceProvider, ICoreSourceSettings
    {
        private readonly IUserSettings userSettings;
        private string? basePath;

        public CoreSourceProvider(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            basePath = userSettings.Get<CoreSourceSettingsData>().sources;
        }

        private string ToAbsolutePath(string path) => Path.Join(basePath, path);

        public string? ReadFile(string path)
        {
            if (!FileExists(path))
                return null;

            return File.ReadAllText(ToAbsolutePath(path));
        }

        public IEnumerable<string> ReadLines(string path)
        {
            if (!FileExists(path))
                return Enumerable.Empty<string>();

            return File.ReadLines(ToAbsolutePath(path));
        }

        public IEnumerable<string> GetFilesInDirectory(string directory, string extension)
        {
            if (basePath == null)
                return Enumerable.Empty<string>();

            if (!DirectoryExists(directory))
                return Enumerable.Empty<string>();
            
            return Directory
                .GetFiles(ToAbsolutePath(directory), $"*.{extension}")
                .Select(f => Path.GetRelativePath(basePath, f));
        }
        
        public IEnumerable<string> GetDirectoriesInDirectory(string directory)
        {
            if (basePath == null)
                return Enumerable.Empty<string>();

            if (!DirectoryExists(directory))
                return Enumerable.Empty<string>();

            return Directory
                .GetDirectories(ToAbsolutePath(directory))
                .Select(f => Path.GetRelativePath(basePath, f));
        }

        public bool SaveFile(string path, string file)
        {
            if (basePath == null)
                return false;

            File.WriteAllText(ToAbsolutePath(path), file);
            
            return true;
        }

        public bool DirectoryExists(string path)
        {
            if (basePath == null)
                return false;
            
            return Directory.Exists(ToAbsolutePath(path));
        }

        public bool FileExists(string path)
        {
            if (basePath == null)
                return false;
            
            return File.Exists(ToAbsolutePath(path));
        }

        public bool SetCorePath(string? path)
        {
            var oldBasePath = basePath;
            basePath = path == null ? null : Path.GetFullPath(path);
            if (path != null)
            {
                if (!DirectoryExists("/"))
                {
                    basePath = oldBasePath;
                    return false;
                }

                if (Path.GetFileName(path) == "src" || Path.GetFileName(path) == "sql")
                    basePath = Path.GetFullPath(Path.Join(basePath, "../"));
                
                if (!DirectoryExists("src/"))
                {
                    basePath = oldBasePath;
                    return false;
                }
                
                if (!DirectoryExists("sql/"))
                {
                    basePath = oldBasePath;
                    return false;
                }
            }

            userSettings.Update(new CoreSourceSettingsData(){sources = basePath});
            return true;
        }

        public string? CurrentCorePath => basePath;
    }

    public struct CoreSourceSettingsData : ISettings
    {
        public string? sources;
    }
}