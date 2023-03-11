using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.Common.Avalonia.Services;

[AutoRegister]
[SingleInstance]
internal class SpellIconDatabase : ISpellIconDatabase
{
    private readonly IFileSystem fileSystem;
    private HttpClient client;
    private LRUCache<uint, Bitmap> cache;
    private SortedDictionary<uint, uint> spellIdToFileId = new();

    private static string SpellIconsPath = "/common/spell_icons/";
    private static string SpellIconsListPath = "/common/spell_icons/spells.txt";
    
    public SpellIconDatabase(IHttpClientFactory factory, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        cache = new LRUCache<uint, Bitmap>(1000);
        client = factory.Factory();
    }

    private bool cacheInitialized;
    private Task? cachingTask = null;

    private static async Task<SortedDictionary<uint, uint>> OpenCache(IFileSystem fileSystem, HttpClient client)
    {
        SortedDictionary<uint, uint> cache = new SortedDictionary<uint, uint>();
        
        if (!Directory.Exists(fileSystem.ResolvePhysicalPath(SpellIconsPath).FullName))
        {
            var tempPath = Path.GetTempFileName() + "d";
            Directory.CreateDirectory(tempPath);
            
            var spellsZip = await client.GetByteArrayAsync("https://dbeditor.ovh/static/textures/spells.zip").ConfigureAwait(false);
            using var zip = new ZipArchive(new MemoryStream(spellsZip), ZipArchiveMode.Read);
            foreach(var entry in zip.Entries)
            {
                await using var stream = entry.Open();
                var destFile = fileSystem.OpenWrite(Path.Combine(tempPath, entry.Name));
                await stream.CopyToAsync(destFile).ConfigureAwait(false);
            }
            
            Directory.Move(tempPath, fileSystem.ResolvePhysicalPath(SpellIconsPath).FullName);
        }
        
        var lines = await fileSystem.ReadAllLinesAsync(SpellIconsListPath).ConfigureAwait(false);
        foreach (var line in lines)
        {
            var split = line.Split(',');
            if (split.Length != 2)
                continue;

            if (!uint.TryParse(split[0], out var spellId) ||
                !uint.TryParse(split[1], out var fileId))
                continue;
            
            cache[spellId] = fileId;
        }

        return cache;
    }

    private async Task PrepareCache()
    {
        if (cacheInitialized)
            return;
        
        if (cachingTask != null)
        {
            await cachingTask;
            return;
        }
        var cachingCompletionSource = new TaskCompletionSource();
        cachingTask = cachingCompletionSource.Task;

        spellIdToFileId = await OpenCache(fileSystem, client);
        
        cacheInitialized = true;
        cachingCompletionSource.SetResult();
    }
    
    public async Task<Bitmap?> GetIcon(uint spellId, CancellationToken cancellationToken = default)
    {
        await PrepareCache();

        if (cancellationToken.IsCancellationRequested)
            return null;
        
        if (!spellIdToFileId.TryGetValue(spellId, out var fileId))
            return null;
        
        if (cache.TryGetValue(fileId, out var bitmap))
            return bitmap;

        if (fileId == 0)
            return null;

        byte[] imageBytes = fileSystem.ReadAllBytes(Path.Join(SpellIconsPath, fileId + ".png"));
        
        if (cancellationToken.IsCancellationRequested)
            return null;
        
        bitmap = new Bitmap(new MemoryStream(imageBytes));
        cache[fileId] = bitmap;
        return bitmap;   
    }

    public bool TryGetCached(uint spellId, out Bitmap? bitmap)
    {
        if (spellIdToFileId.TryGetValue(spellId, out var fileId))
            return cache.TryGetValue(fileId, out bitmap);
        bitmap = null;
        return false;
    }
}