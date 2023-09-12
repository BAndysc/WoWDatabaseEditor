using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.Common.Avalonia.Services;

[AutoRegister]
[SingleInstance]
public class ItemIconsService : IItemIconsService
{
    private readonly IParameterFactory parameterFactory;
    private readonly IFileSystem fileSystem;
    private readonly IDatabaseProvider databaseProvider;
    private readonly string URL = "https://wow.zamimg.com/images/wow/icons/small/";
    private HashSet<string> everLoaded = new HashSet<string>();
    private Dictionary<string, IImage> downloadedIcons = new Dictionary<string, IImage>();

    private Queue<string> icons = new Queue<string>();
    private HttpClient httpClient;

    private IParameter<long>? itemDisplayInfoParameter;
    private Dictionary<uint, uint> itemToDisplayInfo = new();
    private bool isDownloading;

    public ItemIconsService(IParameterFactory parameterFactory,
        IFileSystem fileSystem,
        IDatabaseProvider databaseProvider)
    {
        this.parameterFactory = parameterFactory;
        this.fileSystem = fileSystem;
        this.databaseProvider = databaseProvider;
        httpClient = new HttpClient();
        BuildItemDisplayInfoStore().ListenErrors();
        Directory.CreateDirectory(fileSystem.ResolvePhysicalPath("~/icons").FullName);
    }

    private async Task BuildItemDisplayInfoStore()
    {
        var items = await databaseProvider.GetItemTemplatesAsync();
        if (items == null)
            return;
        foreach (var item in items)
            itemToDisplayInfo[item.Entry] = item.DisplayId;
    }

    private async Task DownloadIcon()
    {
        Debug.Assert(!isDownloading);
        isDownloading = true;
        while (icons.Count > 0)
        {
            var next = icons.Dequeue();
            try
            {
                var bytes = await httpClient.GetByteArrayAsync(URL + next.ToLower() + ".jpg");
                var image = new Bitmap(new MemoryStream(bytes));
                downloadedIcons[next] = image;
                fileSystem.WriteAllBytes("~/icons/" + next + ".jpg", bytes);
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't load icon " + next + ". " + e.Message);
            }
        }

        isDownloading = false;
    }

    public IImage? GetIcon(string name)
    {
        if (downloadedIcons.TryGetValue(name, out var image))
            return image;
        if (!everLoaded.Add(name))
            return null;
        if (fileSystem.Exists("~/icons/" + name + ".jpg"))
        {
            image = new Bitmap(fileSystem.ResolvePhysicalPath("~/icons/" + name + ".jpg").FullName);
            downloadedIcons[name] = image;
            return image;
        }
        icons.Enqueue(name);
        if (!isDownloading)
            DownloadIcon().ListenErrors();
        return null;
    }

    public IImage? GetIcon(uint itemId)
    {
        if (itemDisplayInfoParameter == null)
        {
            if (!parameterFactory.IsRegisteredLong("ItemDisplayInfoParameter"))
                return null;

            itemDisplayInfoParameter = parameterFactory.Factory("ItemDisplayInfoParameter");
        }

        if (!itemToDisplayInfo.TryGetValue(itemId, out var displayId))
            return null;

        if (!itemDisplayInfoParameter.Items!.TryGetValue(displayId, out var displayName))
            return null;

        return GetIcon(displayName.Name);
    }
}