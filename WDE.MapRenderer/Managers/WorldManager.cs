using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class WorldManager : System.IDisposable
{
    private readonly IGameContext gameContext;
    private int currentLoadedMap = -1;
    
    public WorldManager(IGameContext gameContext)
    {
        this.gameContext = gameContext;
        Update(0); // doing first step in the constructor, to load map before other things!
    }

    public void Update(float delta)
    {
        if (currentLoadedMap != gameContext.CurrentMap.Id)
        {
            currentLoadedMap = gameContext.CurrentMap.Id;
            LoadMap();
        }
    }

    private void LoadMap()
    {
        var fullName = $"World\\Maps\\{gameContext.CurrentMap.Directory}\\{gameContext.CurrentMap.Directory}.wdt";
        var wdtBytes = gameContext.ReadFileSync(fullName);
        if (wdtBytes == null)
        {
            Console.WriteLine("Couldn't load map " + fullName + ". This is quite fatal...");
            return;
        }
        var wdt = new WDT(new MemoryBinaryReader(wdtBytes));

        int i = 0;
        Vector3 middlePosSum = Vector3.Zero;
        int chunks = 0;
        foreach (var chunk in wdt.Chunks)
        {
            if (chunk.HasAdt)
            {
                middlePosSum += chunk.MiddlePosition;
                chunks++;
            }
        }
        if (chunks > 0)
        {
            var avg = middlePosSum / chunks;
            gameContext.CameraManager?.Relocate(avg.ToOpenGlPosition().WithY(100));
        }
    }

    public void Dispose()
    {
    }
}