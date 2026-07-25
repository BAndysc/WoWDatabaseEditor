using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WDE.Common.Services;

namespace WDE.DbScriptsEditor.Test
{
    // tests run on desktop only, so plain file access is fine here
    internal class TestRuntimeDataService : IRuntimeDataService
    {
        public Task<string> ReadAllText(string path) => File.ReadAllTextAsync(path);

        public Task<byte[]> ReadAllBytes(string path) => File.ReadAllBytesAsync(path);

        public Task<IReadOnlyList<string>> GetAllFiles(string directory, string searchPattern) =>
            Task.FromResult<IReadOnlyList<string>>(Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories));

        public Task<bool> Exists(string path) => Task.FromResult(File.Exists(path));

        public IDirectoryWatcher WatchDirectory(string path, bool recursive) => throw new System.NotSupportedException();
    }
}
