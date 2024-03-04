using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.RuntimeData;

[AutoRegister(Platforms.NonBrowser)]
[SingleInstance]
public class LocalRuntimeDataService : IRuntimeDataService
{
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

    public async Task<IReadOnlyList<string>> GetAllFiles(string directory, string searchPattern)
    {
        try
        {
            return await Task.FromResult(Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories));
        }
        catch (Exception e)
        {
            throw new DataMissingException(directory, e);
        }
    }

    public async Task<bool> Exists(string path)
    {
        return File.Exists(path);
    }
}