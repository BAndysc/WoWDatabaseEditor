using System.Diagnostics;
using Nito.AsyncEx;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class GameFiles : IGameFiles, IDisposable
{
    private static SemaphoreSlim semaphore = null!;
    private readonly IMpqService mpqService;
    private readonly IMessageBoxService messageBoxService;
    private IMpqArchive mpqSync;
    
    private List<IMpqArchive> mpqPool = new List<IMpqArchive>();

    public GameFilesVersion WoWVersion { get; private set; }
    
    public GameFiles(IMpqService mpqService,
        IMessageBoxService messageBoxService)
    {
        this.mpqService = mpqService;
        this.messageBoxService = messageBoxService;
    }

    public bool Initialize()
    {
        return TryOpenMpq(mpqPool, out mpqSync);
    }

    private bool TryOpenMpq(List<IMpqArchive> archives, out IMpqArchive syncArchive)
    {
        syncArchive = null!;
        try
        {
            syncArchive = mpqService.Open();
            WoWVersion = mpqService.Version ?? GameFilesVersion.Wrath_3_3_5a;
            int asyncCount = syncArchive.Library == MpqLibrary.Managed ? 20 : 1;
            semaphore = new SemaphoreSlim(asyncCount);
            for (int i = 0; i < asyncCount; ++i)
                archives.Add(syncArchive.Clone());
            return true;
        }
        catch (Exception e)
        {
            messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Invalid MPQ")
                .SetMainInstruction("Couldn't parse game MPQ.")
                .SetContent(e.Message + "\n\nAre you using modified game files?")
                .WithButton("Ok", false)
                .Build());
            foreach (var arch in archives)
                arch.Dispose();
            archives.Clear();
            syncArchive = null!;
            return false;
        }
    }

    public async Task<PooledArray<byte>?> ReadFile(FileId fileId, bool silent = false, int? maxReadBytes = null)
    {
        lock (mpqSync)
        {
            var size = mpqSync.GetFileSize(fileId.ToString());
            if (!size.HasValue)
            {
                if (!silent)
                    Console.WriteLine("File " + fileId + " is unreadable");
                return null;
            }

            if (size < 50_000)
            {
                var b = mpqSync.ReadFilePool(fileId, maxReadBytes: maxReadBytes);
                return b;
            }
        }

        await semaphore.WaitAsync();
        Debug.Assert(mpqPool.Count > 0);
        IMpqArchive archive = mpqPool[^1];
        mpqPool.RemoveAt(mpqPool.Count - 1);
        var bytes = await Task.Run(() => archive.ReadFilePool(fileId, maxReadBytes: maxReadBytes));
        mpqPool.Add(archive);
        semaphore.Release();
        
        if (bytes == null && !silent)
            Console.WriteLine("File " + fileId + " is unreadable");
        return bytes;
    }

    public byte[]? ReadFileSync(FileId fileId)
    {
        var bytes = mpqSync.ReadFile(fileId);
        if (bytes == null)
            Console.WriteLine("File " + fileId + " is unreadable");
        return bytes;
    }

    public byte[]? ReadFileSyncLocked(FileId fileId, bool silent = false)
    {
        byte[]? bytes;
        lock (mpqSync)
        {
            bytes = mpqSync.ReadFile(fileId);
        }
        if (bytes == null && !silent)
            Console.WriteLine("File " + fileId + " is unreadable");
        return bytes;
    }

    public string Adt(string mapName, int x, int y) => $"World\\Maps\\{mapName}\\{mapName}_{x}_{y}.adt";
    
    public string AdtTex0(string mapName, int x, int y) => $"World\\Maps\\{mapName}\\{mapName}_{x}_{y}_tex0.adt";
    
    public string AdtObj0(string mapName, int x, int y) => $"World\\Maps\\{mapName}\\{mapName}_{x}_{y}_obj0.adt";
    
    public string AdtLod0(string mapName, int x, int y) => $"World\\Maps\\{mapName}\\{mapName}_{x}_{y}_lod.adt";
    
    public string Wdt(string mapName) => $"World\\Maps\\{mapName}\\{mapName}.wdt";

    public string Wdl(string mapName) => $"World\\Maps\\{mapName}\\{mapName}.wdl";

    public PooledArray<byte>? ReadFileSyncPool(FileId fileId)
    {
        var bytes = mpqSync.ReadFilePool(fileId);
        if (bytes == null)
            Console.WriteLine("File " + fileId + " is unreadable");
        return bytes;
    }

    public void Dispose()
    {
        foreach (var arch in mpqPool)
            arch.Dispose();
        mpqPool.Clear();
        mpqSync.Dispose();
    }
}