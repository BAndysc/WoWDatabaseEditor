using System.Collections;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class WorldManager : System.IDisposable
{
    private readonly IGameFiles gameFiles;
    private readonly IGameContext gameContext;
    private readonly CameraManager cameraManager;
    private readonly NotificationsCenter notificationsCenter;
    private readonly ZoneAreaManager zoneAreaManager;
    private readonly AreaTableStore areaTableStore;
    private AdtChunkType[,] presentChunks = new AdtChunkType[64, 64];
    private Vector3? teleportPosition;
    
    public WorldManager(IGameFiles gameFiles,
        IGameContext gameContext,
        CameraManager cameraManager,
        NotificationsCenter notificationsCenter,
        ZoneAreaManager zoneAreaManager,
        AreaTableStore areaTableStore)
    {
        this.gameFiles = gameFiles;
        this.gameContext = gameContext;
        this.cameraManager = cameraManager;
        this.notificationsCenter = notificationsCenter;
        this.zoneAreaManager = zoneAreaManager;
        this.areaTableStore = areaTableStore;
    }
    
    public WDT? CurrentWdt { get; private set; }
    public WDL? CurrentWdl { get; private set; }

    private int? prevAreaId;
    public void Update(float delta)
    {
        var areaId = zoneAreaManager.GetAreaId(gameContext.CurrentMap.Id, cameraManager.Position);
        if (areaId != prevAreaId)
        {
            prevAreaId = areaId;
            if (areaId.HasValue)
            {
                if (areaTableStore.Contains((uint)areaId.Value))
                {
                    var areaName = areaTableStore[(uint)areaId.Value];
                    notificationsCenter.ShowMessage(areaName.Name);
                }
            }
        }
    }

    public IEnumerator LoadOptionals(CancellationToken cancel)
    {
        yield break;
    }
    
    public IEnumerator LoadMap(CancellationToken cancel)
    {
        var wdtPath = gameFiles.Wdt(gameContext.CurrentMap.Directory);
        var wdlPath = gameFiles.Wdl(gameContext.CurrentMap.Directory);
        var wdtBytesTask = gameFiles.ReadFile(wdtPath);
        var wdlBytesTask = gameFiles.ReadFile(wdlPath);
        yield return wdtBytesTask;
        yield return wdlBytesTask;

        using var wdtBytes = wdtBytesTask.Result;
        using var wdlBytes = wdlBytesTask.Result;
        if (wdtBytes == null)
        {
            Console.WriteLine("Couldn't load map " + wdtPath + ". This is quite fatal...");
            yield break;
        }
        
        ClearData();
        
        CurrentWdt = new WDT(new MemoryBinaryReader(wdtBytes), gameFiles.WoWVersion);
        if (wdlBytes != null)
            CurrentWdl = new WDL(new MemoryBinaryReader(wdlBytes));
        else
            CurrentWdl = null;

        Vector3 middlePosSum = Vector3.Zero;
        int chunks = 0;
        foreach (var chunk in CurrentWdt.Chunks)
        {
            presentChunks[chunk.Y, chunk.X] = chunk.HasAdt ? (chunk.IsAllWater ? AdtChunkType.AllWater : AdtChunkType.Regular) : AdtChunkType.None;
            if (chunk.HasAdt)
            {
                middlePosSum += chunk.MiddlePosition;
                chunks++;
            }
        }
        
        if (teleportPosition.HasValue)
        {
            cameraManager.Relocate(teleportPosition.Value);
            teleportPosition = null;
        }
        else if (chunks > 0)
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
                cameraManager.Relocate(CurrentWdt.WorldMapObject.Value.pos.WithZ(100));
            }
        }
    }

    private void ClearData()
    {
        for (int y = 0; y < 64; ++y)
        {
            for (int x = 0; x < 64; ++x)
            {
                presentChunks[y, x] = AdtChunkType.None;
            }
        }
    }

    public void Dispose()
    {
    }

    public bool IsChunkPresent(int chunkX, int chunkY, out AdtChunkType type)
    {
        type = default;
        if (chunkX < 0 || chunkX >= 64 || chunkY < 0 || chunkY >= 64 || presentChunks[chunkY, chunkX] == AdtChunkType.None)
            return false;
        type = presentChunks[chunkY, chunkX];
        return true;
    }

    public void SetNextTeleportPosition(Vector3? teleportPosition) => this.teleportPosition = teleportPosition;
}