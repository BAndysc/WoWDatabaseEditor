using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.Common.Avalonia.Services;

[AutoRegister]
[SingleInstance]
internal class ItemIconDatabase : IItemIconsService
{
    private readonly IFileSystem fileSystem;
    private readonly IMessageBoxService messageBoxService;
    private HttpClient client;
    private LRUCache<uint, Bitmap> cache;
    private SortedDictionary<int, uint> itemIdToFileId = new();

    private static string ItemIconsPath = "/common/item_icons/";
    private static string ItemIconsListPath = "/common/item_icons/icons.txt";
    private static string ItemIconsVersionPath = "/common/item_icons/version.txt";
    
    public ItemIconDatabase(IHttpClientFactory factory, 
        IFileSystem fileSystem,
        IMessageBoxService messageBoxService)
    {
        this.fileSystem = fileSystem;
        this.messageBoxService = messageBoxService;
        cache = new LRUCache<uint, Bitmap>(1000);
        client = factory.Factory();
    }

    private bool cacheInitialized;
    private Task? cachingTask = null;

    private async Task<SortedDictionary<int, uint>> OpenCache()
    {
        SortedDictionary<int, uint> cache = new SortedDictionary<int, uint>();
        
        if (!Directory.Exists(fileSystem.ResolvePhysicalPath(ItemIconsPath).FullName) ||
            !fileSystem.Exists(ItemIconsVersionPath) ||
            fileSystem.ReadAllText(ItemIconsVersionPath) != "1")
        {
            if (!await AskIfDownload())
                return cache;
            
            var tempPath = Path.ChangeExtension(Path.GetTempFileName(), null);
            Directory.CreateDirectory(tempPath);
            
            var itemsZip = await client.GetByteArrayAsync("https://dbeditor.ovh/static/textures/item_icons.zip").ConfigureAwait(false);
            using var zip = new ZipArchive(new MemoryStream(itemsZip), ZipArchiveMode.Read);
            foreach(var entry in zip.Entries)
            {
                await using var stream = entry.Open();
                var destFile = fileSystem.OpenWrite(Path.Combine(tempPath, entry.Name));
                await stream.CopyToAsync(destFile).ConfigureAwait(false);
                await destFile.DisposeAsync();
            }

            var dest = fileSystem.ResolvePhysicalPath(ItemIconsPath);
            var destPath = dest.FullName;

            LOG.LogInformation("Copying new icons to -> " + destPath);
            if (Directory.Exists(destPath))
                Directory.Delete(destPath, true);
            Directory.CreateDirectory(destPath);
            
            foreach (var file in Directory.GetFiles(tempPath))
                File.Move(file, Path.Join(destPath, Path.GetFileName(file)));
            
            fileSystem.WriteAllText(ItemIconsVersionPath, "1");
        }
        
        var lines = await fileSystem.ReadAllLinesAsync(ItemIconsListPath).ConfigureAwait(false);
        foreach (var line in lines)
        {
            var split = line.Split(',');
            if (split.Length != 2)
                continue;

            if (!int.TryParse(split[0], out var itemId) ||
                !uint.TryParse(split[1], out var fileId))
                continue;
            
            cache[itemId] = fileId;
        }

        return cache;
    }

    private Task<bool> AskIfDownload()
    {
        return messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Download icons")
            .SetMainInstruction("Download additional icons")
            .SetContent("To display items' icons, the editor needs to download them first. It only needs to be done once, around 90 MBs to download. Do you want to download it now")
            .WithYesButton(true)
            .WithNoButton(false)
            .Build());
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

        itemIdToFileId = await OpenCache();
        
        cacheInitialized = true;
        cachingCompletionSource.SetResult();
    }

    public Task<IImage?> GetItemIcon(uint itemId, CancellationToken token) => GetIcon((int)itemId, token);

    public bool TryGetCachedItemIcon(uint itemId, out IImage? image) => TryGetCachedIcon((int)itemId, out image);

    public Task<IImage?> GetCurrencyIcon(uint currencyId, CancellationToken token) => GetIcon(-(int)currencyId, token);

    public bool TryGetCachedCurrencyIcon(uint currencyId, out IImage? image) => TryGetCachedIcon(-(int)currencyId, out image);
    
    public async Task<IImage?> GetIcon(int itemOrCurrencyId, CancellationToken cancellationToken = default)
    {
        await PrepareCache();

        if (cancellationToken.IsCancellationRequested)
            return null;
        
        if (!itemIdToFileId.TryGetValue(itemOrCurrencyId, out var fileId))
            return null;
        
        if (cache.TryGetValue(fileId, out var bitmap))
            return bitmap;

        if (fileId == 0)
            return null;

        byte[] imageBytes = fileSystem.ReadAllBytes(Path.Join(ItemIconsPath, fileId + ".png"));
        
        if (cancellationToken.IsCancellationRequested)
            return null;
        
        bitmap = new Bitmap(new MemoryStream(imageBytes));
        cache[fileId] = bitmap;
        return bitmap;   
    }

    public bool TryGetCachedIcon(int itemOrCurrencyId, out IImage? image)
    {
        if (itemIdToFileId.TryGetValue(itemOrCurrencyId, out var fileId))
        {
            var has = cache.TryGetValue(fileId, out var bitmap);
            image = bitmap;
            return has;
        }
        image = null;
        return false;
    }
}