using Nito.AsyncEx;
using TheEngine;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class GameFiles : IGameFiles, IDisposable
{
    private static SemaphoreSlim semaphore = null!;
    private readonly IMpqService mpqService;
    private readonly IMessageBoxService messageBoxService;
    private readonly IMainThread mainThread;
    private readonly Engine engine;
    private IMpqArchive mpqSync;

    // Reads run concurrently on separate cloned archive handles (thread-safe across distinct handles),
    // cloned lazily up to maxArchives so we only pay the archive-open cost for concurrency actually used.
    private int maxArchives;
    private readonly Stack<IMpqArchive> availableArchives = new();
    private readonly List<IMpqArchive> allArchives = new(); // every clone, for disposal
    private readonly object archivePoolLock = new();
    private readonly object cloneLock = new();

    public GameFilesVersion WoWVersion { get; private set; }
    
    public GameFiles(IMpqService mpqService,
        IMessageBoxService messageBoxService,
        IMainThread mainThread,
        Engine engine)
    {
        this.mpqService = mpqService;
        this.messageBoxService = messageBoxService;
        this.mainThread = mainThread;
        this.engine = engine;
    }

    public bool Initialize()
    {
        return TryOpenMpq(out mpqSync);
    }

    private bool TryOpenMpq(out IMpqArchive syncArchive)
    {
        syncArchive = null!;
        try
        {
            syncArchive = mpqService.Open();
            WoWVersion = mpqService.Version ?? GameFilesVersion.Wrath_3_3_5a;
            // mpqSync stays out of the async pool (ReadFileSyncLocked uses it under lock); clones are lazy
            maxArchives = syncArchive.Library == MpqLibrary.Managed ? 20 : 8;
            semaphore = new SemaphoreSlim(maxArchives);
            return true;
        }
        catch (Exception e)
        {
            var message = e.Message;
            // TryOpenMpq runs on the game thread, dialogs must be created on the UI thread
            mainThread.Dispatch(() => messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Invalid MPQ")
                .SetMainInstruction("Couldn't parse game MPQ.")
                .SetContent(message + "\n\nAre you using modified game files?")
                .WithButton("Ok", false)
                .Build()).ListenErrors());
            syncArchive?.Dispose();
            syncArchive = null!;
            return false;
        }
    }

    public async ValueTask<PooledArray<byte>?> ReadFile(FileId fileId, bool silent = false, int? maxReadBytes = null)
    {
        // uncomment to make loading faster, but laggier
        // hard to decide if it's worth it
        /*lock (mpqSync)
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
        }*/
        await semaphore.WaitAsync();
        await engine.EnterThreadPool;

        // free clone, or lazily create one (serialized; the semaphore caps total clones at maxArchives)
        IMpqArchive archive;
        lock (archivePoolLock)
            archive = availableArchives.Count > 0 ? availableArchives.Pop() : null!;
        if (archive == null)
        {
            lock (cloneLock)
                archive = mpqSync.Clone();
            lock (archivePoolLock)
                allArchives.Add(archive);
        }

        var bytes = archive.ReadFilePool(fileId, maxReadBytes: maxReadBytes);

        lock (archivePoolLock)
            availableArchives.Push(archive);
        semaphore.Release();

        await engine.EnterGameLoop;

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
        lock (archivePoolLock)
        {
            foreach (var arch in allArchives) // lazily-created async clones
                arch.Dispose();
            allArchives.Clear();
            availableArchives.Clear();
        }
        mpqSync.Dispose();
    }
}