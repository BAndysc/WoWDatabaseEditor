using Nito.AsyncEx;
using WDE.Common.MPQ;
using WDE.Common.Services.MessageBox;
using WDE.MpqReader;

namespace WDE.MapRenderer.Managers;

public class GameFiles : IGameFiles, IDisposable
{
    private AsyncMonitor monitor = new ();
    private readonly IMpqService mpqService;
    private readonly IMessageBoxService messageBoxService;
    private IMpqArchive mpq, mpqSync;

    public GameFiles(IMpqService mpqService,
        IMessageBoxService messageBoxService)
    {
        this.mpqService = mpqService;
        this.messageBoxService = messageBoxService;
    }

    public bool Initialize()
    {
        return TryOpenMpq(out mpq, out mpqSync);
    }

    private bool TryOpenMpq(out IMpqArchive m, out IMpqArchive m2) // we open two archive, once for async loading, second for sync loading
    {
        m = null!;
        m2 = null!;
        try
        {
            m = mpqService.Open();
            m2 = mpqService.Open();
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
            m?.Dispose();
            m = null!;
            m2 = null!;
            return false;
        }
    }

    public async Task<PooledArray<byte>?> ReadFile(string fileName)
    {
        using var _ = await monitor.EnterAsync();
        var bytes = await Task.Run(() => mpq.ReadFilePool(fileName));
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
        mpq.Dispose();
        mpqSync.Dispose();
    }
}