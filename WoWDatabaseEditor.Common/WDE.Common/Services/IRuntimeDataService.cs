using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IRuntimeDataService
{
    Task<string> ReadAllText(string path);
    Task<byte[]> ReadAllBytes(string path);
    Task<IReadOnlyList<string>> GetAllFiles(string directory, string searchPattern);
    Task<bool> Exists(string path);
    IDirectoryWatcher WatchDirectory(string path, bool recursive);
}

public interface IDirectoryWatcher : System.IDisposable
{
    event Action<WatcherChangeTypes, string>? OnChanged;
}

public class DataMissingException : Exception
{
    public DataMissingException(string path, Exception? nested) : base("Couldn't read file " + path, nested) { }
}