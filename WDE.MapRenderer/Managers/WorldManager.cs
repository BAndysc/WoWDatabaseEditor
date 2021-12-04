using System.Collections;
using System.Diagnostics;
using Avalonia.Media.TextFormatting.Unicode;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class WorldManager : System.IDisposable
{
    private readonly IGameContext gameContext;
    private uint[,]?[,] areaTable = new uint[,]?[64,64];
    private bool[,] presentChunks = new bool[64, 64];
    
    public WorldManager(IGameContext gameContext)
    {
        this.gameContext = gameContext;
    }

    private uint? prevAreaId;
    public void Update(float delta)
    {
        var areaId = GetAreaId(gameContext.CameraManager.Position.ToWoWPosition());
        if (areaId != prevAreaId)
        {
            prevAreaId = areaId;
            if (areaId.HasValue)
            {
                if (gameContext.DbcManager.AreaTableStore.Contains(areaId.Value))
                {
                    var areaName = gameContext.DbcManager.AreaTableStore[areaId.Value];
                    gameContext.NotificationsCenter.ShowMessage(areaName.Name);
                }
            }
        }
    }

    public uint? GetAreaId(Vector3 wowPosition)
    {
        var chunk = wowPosition.WoWPositionToChunk();
        if (chunk.Item1 < 0 || chunk.Item1 >= 64 || chunk.Item2 < 0 || chunk.Item2 >= 64)
            return null;
        
        var areaIds = areaTable[chunk.Item1, chunk.Item2];
        if (areaIds == null)
            return null;
        
        var chunkPosition = chunk.ChunkToWoWPosition();
        var relativePosition = chunkPosition - wowPosition;
        var x = Math.Clamp((int)(relativePosition.X / Constants.ChunkSize), 0, 63);
        var y = Math.Clamp((int)(relativePosition.Y / Constants.ChunkSize), 0, 63);
        return areaIds[x, y];
    }

    public IEnumerator LoadOptionals(CancellationToken cancel)
    {
        for (int y = 0; y < 64; ++y)
        {
            for (int x = 0; x < 64; ++x)
            {
                if (cancel.IsCancellationRequested)
                    yield break;
            
                if (presentChunks[y, x])
                {
                    var adtFullName = $"World\\Maps\\{gameContext.CurrentMap.Directory}\\{gameContext.CurrentMap.Directory}_{x}_{y}.adt";

                    var adtBytesTask = gameContext.ReadFile(adtFullName);
                    yield return adtBytesTask;
                
                    using var adtBytes = adtBytesTask.Result;
                    if (adtBytes != null)
                    {
                        var adt = new FastAdtAreaTable(new MemoryBinaryReader(adtBytes));
                        areaTable[y, x] = adt.AreaIds;
                    }
                }
            }
        }
    }
    
    public IEnumerator LoadMap(CancellationToken cancel)
    {
        var fullName = $"World\\Maps\\{gameContext.CurrentMap.Directory}\\{gameContext.CurrentMap.Directory}.wdt";
        var wdtBytesTask = gameContext.ReadFile(fullName);
        yield return wdtBytesTask;

        using var wdtBytes = wdtBytesTask.Result;
        if (wdtBytes == null)
        {
            Console.WriteLine("Couldn't load map " + fullName + ". This is quite fatal...");
            yield break;
        }

        ClearData();
        
        var wdt = new WDT(new MemoryBinaryReader(wdtBytes));

        Vector3 middlePosSum = Vector3.Zero;
        int chunks = 0;
        foreach (var chunk in wdt.Chunks)
        {
            presentChunks[chunk.Y, chunk.X] = chunk.HasAdt;
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

    private void ClearData()
    {
        for (int y = 0; y < 64; ++y)
        {
            for (int x = 0; x < 64; ++x)
            {
                areaTable[y, x] = null;
                presentChunks[y, x] = false;
            }
        }
    }

    public void Dispose()
    {
    }
}