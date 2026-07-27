using WDE.Common.Services;

namespace DatabaseTester;

// Minimal local-file IRuntimeDataService so TableDefinitionProvider can load DbDefinitions
// (copied next to the tester binary) - the tester has no interest in file watching.
public class FileRuntimeDataService : IRuntimeDataService
{
    private class NoOpWatcher : IDirectoryWatcher
    {
        public event Action<WatcherChangeTypes, string>? OnChanged;
        public void Dispose() { }
    }

    public IDirectoryWatcher WatchDirectory(string path, bool recursive) => new NoOpWatcher();

    public async Task<string> ReadAllText(string path)
    {
        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception e)
        {
            throw new DataMissingException(path, e);
        }
    }

    public async Task<byte[]> ReadAllBytes(string path)
    {
        try
        {
            return await File.ReadAllBytesAsync(path);
        }
        catch (Exception e)
        {
            throw new DataMissingException(path, e);
        }
    }

    public Task<IReadOnlyList<string>> GetAllFiles(string directory, string searchPattern)
    {
        if (!Directory.Exists(directory))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        return Task.FromResult<IReadOnlyList<string>>(Directory
            .GetFiles(directory, searchPattern, SearchOption.AllDirectories)
            .Select(x => x.Replace("\\", "/"))
            .ToList());
    }

    public Task<bool> Exists(string path) => Task.FromResult(File.Exists(path));
}
