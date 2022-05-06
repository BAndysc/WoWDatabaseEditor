using System.Diagnostics;
using Nito.AsyncEx;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.MpqReader;

namespace WDE.MapRenderer.Managers;

public class GameFiles : IGameFiles, IDisposable
{
    private static SemaphoreSlim semaphore = new(20);
    private readonly IMpqService mpqService;
    private readonly IMessageBoxService messageBoxService;
    private IMpqArchive mpqSync;
    
    private List<IMpqArchive> mpqPool = new List<IMpqArchive>();

    public GameFiles(IMpqService mpqService,
        IMessageBoxService messageBoxService)
    {
        this.mpqService = mpqService;
        this.messageBoxService = messageBoxService;
    }

    public bool Initialize()
    {
        return TryOpenMpq(mpqPool, semaphore.CurrentCount, out mpqSync);
    }

    private bool TryOpenMpq(List<IMpqArchive> archives, int asyncCount, out IMpqArchive syncArchive)
    {
        syncArchive = null!;
        try
        {
            syncArchive = mpqService.Open();
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

    public async Task<PooledArray<byte>?> ReadFile(string fileName)
    {
        await semaphore.WaitAsync();
        Debug.Assert(mpqPool.Count > 0);
        IMpqArchive archive = mpqPool[^1];
        mpqPool.RemoveAt(mpqPool.Count - 1);
        var bytes = await Task.Run(() => archive.ReadFilePool(fileName));
        mpqPool.Add(archive);
        semaphore.Release();
        
        if (bytes == null)
            Console.WriteLine("File " + fileName + " is unreadable");
        return bytes;
    }

    public byte[]? ReadFileSync(string fileName)
    {
        var bytes = mpqSync.ReadFile(fileName);
        if (bytes == null)
            Console.WriteLine("File " + fileName + " is unreadable");
        return bytes;
    }

    public byte[]? ReadFileSyncLocked(string fileName)
    {
        byte[]? bytes;
        lock (mpqSync)
        {
            bytes = mpqSync.ReadFile(fileName);
        }
        if (bytes == null)
            Console.WriteLine("File " + fileName + " is unreadable");
        return bytes;
    }

    public string Adt(string mapName, int x, int y) => $"World\\Maps\\{mapName}\\{mapName}_{x}_{y}.adt";
    
    public string Wdt(string mapName) => $"World\\Maps\\{mapName}\\{mapName}.wdt";

    public PooledArray<byte>? ReadFileSyncPool(string fileName)
    {
        var bytes = mpqSync.ReadFilePool(fileName);
        if (bytes == null)
            Console.WriteLine("File " + fileName + " is unreadable");
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