using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Services;

public class WebItemIconsService // : IItemIconsService
{
    private readonly IParameterFactory parameterFactory;
    private readonly IFileSystem fileSystem;
    private readonly IItemStore itemStore;
    private readonly IDatabaseProvider databaseProvider;
    private static string ItemIconsPath = "/common/item_icons/";
    private readonly string URL = "https://wow.zamimg.com/images/wow/icons/small/";
    private readonly string FILE_ID_URL = "https://www.wowhead.com/icon=";
    private Dictionary<string, IImage?> cachedIcons = new();
    private Dictionary<uint, IImage?> cachedIconsByFileId = new();
    
    private Dictionary<string, Task<IImage?>> pendingIcons = new();
    private Dictionary<uint, Task<IImage?>> pendingIconsByFileId = new();
    //         
    // private HashSet<uint> fileIdEverLoaded = new HashSet<uint>();
    // private HashSet<string> everLoaded = new HashSet<string>();

    private Queue<(uint, TaskCompletionSource<IImage?>)> fileIdsQueue = new ();
    private Queue<(string, TaskCompletionSource<IImage?>)> pathsQueue = new ();
    private HttpClient httpClient;

    private Dictionary<uint, string> itemToPathName = new(); // pre legion
    private bool isDownloading;

    public WebItemIconsService(IParameterFactory parameterFactory,
        IFileSystem fileSystem,
        IItemStore itemStore,
        IDatabaseProvider databaseProvider)
    {
        this.parameterFactory = parameterFactory;
        this.fileSystem = fileSystem;
        this.itemStore = itemStore;
        this.databaseProvider = databaseProvider;
        httpClient = new HttpClient();
        BuildItemDisplayInfoStore().ListenErrors();
        Directory.CreateDirectory(fileSystem.ResolvePhysicalPath("~/icons").FullName);
    }

    private async Task BuildItemDisplayInfoStore()
    {
        var items = await databaseProvider.GetItemTemplatesAsync();
        foreach (var item in itemStore.Items)
        {
            if (item.UsesFileId)
                break;

            if (item.DisplayInfo is {} displayInfo &&
                displayInfo.UsesFilePath)
                itemToPathName[item.Id] = displayInfo.InventoryIconPath!;
        }
        if (items != null)
        {
            foreach (var item in items)
            {
                if (itemStore.GetItemDisplayInfoById(item.DisplayId) is { } displayInfo &&
                    displayInfo.UsesFilePath)
                    itemToPathName[item.Entry] = displayInfo.InventoryIconPath!;
            }
        }
    }

    private async Task DownloadIcon()
    {
        Debug.Assert(!isDownloading);
        isDownloading = true;
        
        while (fileIdsQueue.Count > 0)
        {
            var (next, tcs) = fileIdsQueue.Dequeue();
            try
            {
                var html = await httpClient.GetStringAsync(FILE_ID_URL + next);
                var indexOf = html.IndexOf("<meta property=\"og:image\" content=\"");
                if (indexOf >= 0)
                {
                    var indexOf2 = html.IndexOf("/large/", indexOf);
                    var end = html.IndexOf(".jpg", indexOf2);
                    var path = html.Substring(indexOf2 + 7, end - indexOf2 - 7);
                    
                    var bytes = await httpClient.GetByteArrayAsync(URL + path.ToLower() + ".jpg");
                    var image = new Bitmap(new MemoryStream(bytes));
                    cachedIconsByFileId[next] = image;
                    tcs.SetResult(image);
                    pendingIconsByFileId.Remove(next);
                    fileSystem.WriteAllBytes("~/icons/" + next + ".jpg", bytes);
                }
            }
            catch (Exception e)
            {
                LOG.LogWarning("Couldn't load icon " + next + ". " + e.Message);
                cachedIconsByFileId[next] = null;
                tcs.SetResult(null);
                pendingIconsByFileId.Remove(next);
            }
        }
        
        while (pathsQueue.Count > 0)
        {
            var (next, tcs) = pathsQueue.Dequeue();
            try
            {
                var bytes = await httpClient.GetByteArrayAsync(URL + next.ToLower() + ".jpg");
                var image = new Bitmap(new MemoryStream(bytes));
                cachedIcons[next] = image;
                tcs.SetResult(image);
                pendingIcons.Remove(next);
                fileSystem.WriteAllBytes("~/icons/" + next + ".jpg", bytes);
            }
            catch (Exception e)
            {
                LOG.LogWarning("Couldn't load icon " + next + ". " + e.Message);
                cachedIcons[next] = null;
                tcs.SetResult(null);
                pendingIcons.Remove(next);
            }
        }

        isDownloading = false;
    }

    public async Task<IImage?> GetItemIcon(uint itemId)
    {
        if (GetCachedItemIcon(itemId, out var image))
            return image;
        
        if (itemStore.GetItemById(itemId) is not { } item)
            return null;

        if (item.UsesFileId)
        {
            var fileId = item.InventoryIconFileDataId!.Value;

            if (fileId == 0)
                return null;
            
            if (pendingIconsByFileId.TryGetValue(fileId, out var task))
                return await task;

            var tcs = new TaskCompletionSource<IImage?>();

            pendingIconsByFileId[fileId] = tcs.Task;
            fileIdsQueue.Enqueue((fileId, tcs));

            if (!isDownloading)
                await DownloadIcon();
            
            return await tcs.Task;
        }
        else if (item.UsesFilePath)
        {
            if (!itemToPathName.TryGetValue(itemId, out var pathName))
                return null;

            if (pendingIcons.TryGetValue(pathName, out var task2))
                return await task2;
            
            var tcs = new TaskCompletionSource<IImage?>();

            pendingIcons[pathName] = tcs.Task;
            pathsQueue.Enqueue((pathName, tcs));

            if (!isDownloading)
                await DownloadIcon();
            
            return await tcs.Task;
        }
        else
            return null;
    }

    public async Task<IImage?> GetCurrencyIcon(uint currencyId)
    {
        if (GetCachedCurrencyIcon(currencyId, out var image))
            return image;
        
        if (itemStore.GetCurrencyTypeById(currencyId) is not { } currency)
            return null;

        if (currency.UsesFileId)
        {
            var fileId = currency.InventoryIconFileId!.Value;
            
            if (fileId == 0)
                return null;
            
            if (pendingIconsByFileId.TryGetValue(fileId, out var task))
                return await task;

            var tcs = new TaskCompletionSource<IImage?>();

            pendingIconsByFileId[fileId] = tcs.Task;
            fileIdsQueue.Enqueue((fileId, tcs));

            if (!isDownloading)
                await DownloadIcon();
            
            return await tcs.Task;
        }
        else if (currency.UsesFilePath)
        {
            var pathName = currency.InventoryIconPath!;

            if (pendingIcons.TryGetValue(pathName, out var task2))
                return await task2;
            
            var tcs = new TaskCompletionSource<IImage?>();

            pendingIcons[pathName] = tcs.Task;
            pathsQueue.Enqueue((pathName, tcs));

            if (!isDownloading)
                await DownloadIcon();
            
            return await tcs.Task;
        }
        else
            return null;
    }
    
    public bool GetCachedItemIcon(uint itemId, out IImage? image)
    {
        image = null;
        if (itemStore.GetItemById(itemId) is not { } item)
            return false;
        
        if (item.UsesFileId)
        {
            var fileId = item.InventoryIconFileDataId!.Value;
            
            if (fileId == 0)
                return true;

            if (cachedIconsByFileId.TryGetValue(fileId, out image))
                return true;
            
            if (fileSystem.Exists(ItemIconsPath + fileId + ".png"))
            {
                image = new Bitmap(fileSystem.ResolvePhysicalPath(ItemIconsPath + fileId + ".png").FullName);
                cachedIconsByFileId[fileId] = image;
                return true;
            }

            return false;
        }
        else if (item.UsesFilePath)
        {
            if (!itemToPathName.TryGetValue(itemId, out var pathName))
                return false;

            if (cachedIcons.TryGetValue(pathName, out image))
                return true;
            
            if (fileSystem.Exists("~/icons/" + pathName + ".jpg"))
            {
                image = new Bitmap(fileSystem.ResolvePhysicalPath("~/icons/" + pathName + ".jpg").FullName);
                cachedIcons[pathName] = image;
                return true;
            }

            return false;
        }
        else
            return false;
    }
    
    public bool GetCachedCurrencyIcon(uint currencyId, out IImage? image)
    {
        image = null;
        if (itemStore.GetCurrencyTypeById(currencyId) is not { } currency)
            return false;
        
        if (currency.UsesFileId)
        {
            var fileId = currency.InventoryIconFileId!.Value;
            
            if (fileId == 0)
                return true;

            if (cachedIconsByFileId.TryGetValue(fileId, out image))
                return true;
            
            if (fileSystem.Exists(ItemIconsPath + fileId + ".png"))
            {
                image = new Bitmap(fileSystem.ResolvePhysicalPath(ItemIconsPath + fileId + ".png").FullName);
                cachedIconsByFileId[fileId] = image;
                return true;
            }

            return false;
        }
        else if (currency.UsesFilePath)
        {
            var pathName = currency.InventoryIconPath!;
            if (cachedIcons.TryGetValue(pathName, out image))
                return true;
            
            if (fileSystem.Exists("~/icons/" + pathName + ".jpg"))
            {
                image = new Bitmap(fileSystem.ResolvePhysicalPath("~/icons/" + pathName + ".jpg").FullName);
                cachedIcons[pathName] = image;
                return true;
            }

            return false;
        }
        else
            return false;
    }
}