using System.Collections;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class WorldManager : System.IDisposable
{
    private readonly IGameFiles gameFiles;
    private readonly IGameContext gameContext;
    private readonly CameraManager cameraManager;
    private readonly NotificationsCenter notificationsCenter;
    private readonly AreaTableStore areaTableStore;
    private uint[,]?[,] areaTable = new uint[,]?[64,64];
    private bool[,] presentChunks = new bool[64, 64];
    
    public WorldManager(IGameFiles gameFiles,
        IGameContext gameContext,
        CameraManager cameraManager,
        NotificationsCenter notificationsCenter,
        AreaTableStore areaTableStore)
    {
        this.gameFiles = gameFiles;
        this.gameContext = gameContext;
        this.cameraManager = cameraManager;
        this.notificationsCenter = notificationsCenter;
        this.areaTableStore = areaTableStore;
    }
    
    public WDT? CurrentWdt { get; private set; }

    private uint? prevAreaId;
    public void Update(float delta)
    {
        var areaId = GetAreaId(cameraManager.Position);
        if (areaId != prevAreaId)
        {
            prevAreaId = areaId;
            if (areaId.HasValue)
            {
                if (areaTableStore.Contains(areaId.Value))
                {
                    var areaName = areaTableStore[areaId.Value];
                    notificationsCenter.ShowMessage(areaName.Name);
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
                    var adtBytesTask = gameFiles.ReadFile(gameFiles.Adt(gameContext.CurrentMap.Directory, x, y));
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
        var fullName = gameFiles.Wdt(gameContext.CurrentMap.Directory);
        var wdtBytesTask = gameFiles.ReadFile(fullName);
        yield return wdtBytesTask;

        using var wdtBytes = wdtBytesTask.Result;
        if (wdtBytes == null)
        {
            Console.WriteLine("Couldn't load map " + fullName + ". This is quite fatal...");
            yield break;
        }

        ClearData();
        
        CurrentWdt = new WDT(new MemoryBinaryReader(wdtBytes));

        Vector3 middlePosSum = Vector3.Zero;
        int chunks = 0;
        foreach (var chunk in CurrentWdt.Chunks)
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
            if (gameContext.CurrentMap.Id == 1)
            {
                // this is just for debugging
                // since the beginning this was the initial position in Kalimdor
                // and it is quite nice starting position
                cameraManager.Relocate(new Vector3(285.396f, -4746.17f, 9.48428f + 20));
            }
            else
                cameraManager.Relocate(avg.WithZ(100));
        }
        else
        {
            if (CurrentWdt.WorldMapObject != null)
            {
                cameraManager.Relocate(CurrentWdt.WorldMapObject.pos.WithZ(100));
            }
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

    public bool IsChunkPresent(int chunkX, int chunkY)
    {
        return chunkX >= 0 && chunkX < 64 && chunkY >= 0 && chunkY < 64 && presentChunks[chunkY, chunkY];
    }
}