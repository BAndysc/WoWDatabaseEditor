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
    private bool[,] presentChunks = new bool[64, 64];
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

    private uint? prevAreaId;
    public void Update(float delta)
    {
        var areaId = zoneAreaManager.GetAreaId((uint)gameContext.CurrentMap.Id, cameraManager.Position);
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

    public IEnumerator LoadOptionals(CancellationToken cancel)
    {
        yield break;
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

    public void SetNextTeleportPosition(Vector3? teleportPosition) => this.teleportPosition = teleportPosition;
}