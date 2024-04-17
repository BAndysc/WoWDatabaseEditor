using System.Collections;
using System.Diagnostics;
using ImGuiNET;
using WDE.Common.Services;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class ZoneAreaManager
{
    private readonly MapStore mapStore;
    private readonly IFileSystem fileSystem;
    private readonly IGameFiles gameFiles;

    private bool loaded;
    private readonly Dictionary<int, ChunkAreas?[,]> areaTables = new();

    private FileInfo CachePath => fileSystem.ResolvePhysicalPath($"~/cache/areas_{(int)gameFiles.WoWVersion}.bin");
    
    private struct ChunkAreas
    {
        public ushort? OneAreaForAll;
        public ushort[,]? Areas;

        public ushort this[int y, int x]
        {
            get
            {
                if (OneAreaForAll.HasValue)
                    return OneAreaForAll.Value;
                return Areas![y, x];
            }
        }
    }
    
    public ZoneAreaManager(MapStore mapStore,
        IFileSystem fileSystem,
        IGameFiles gameFiles)
    {
        this.mapStore = mapStore;
        this.fileSystem = fileSystem;
        this.gameFiles = gameFiles;
    }
    
    public int? GetAreaId(int map, Vector3 wowPosition)
    {
        if (!areaTables.TryGetValue(map, out var areas))
            return null;
        
        var chunk = wowPosition.WoWPositionToChunk();
        if (chunk.Item1 < 0 || chunk.Item1 >= 64 || chunk.Item2 < 0 || chunk.Item2 >= 64)
            return null;
        
        var areaIds = areas[chunk.Item1, chunk.Item2];
        if (!areaIds.HasValue)
            return null;
        
        var chunkPosition = chunk.ChunkToWoWPosition();
        var relativePosition = chunkPosition - wowPosition;
        var x = Math.Clamp((int)(relativePosition.X / Constants.ChunkSize), 0, 63);
        var y = Math.Clamp((int)(relativePosition.Y / Constants.ChunkSize), 0, 63);
        return areaIds.Value[x, y];
    }

    private class GenerationProgress
    {
        public int Total;
        public int Done;
        public string Current;
    }

    private GenerationProgress? progress;

    public void RenderGUI()
    {
        if (progress == null)
            return;

        ImGui.SetNextWindowSize(new Vector2(400, 170), ImGuiCond.Always);
        ImGui.Begin("First time data generation");
        
        ImGui.TextWrapped("This is the first time you've opened the 3D view, the editor needs to process the files first.\n\nIt will take few minutes, but I promise, it has to be done only once!");

        var frac = progress.Done * 1.0f / progress.Total;
        ImGui.ProgressBar(frac, new Vector2(-1, 0), $"{frac*100:0.00}%");
        
        ImGui.Text("Processing map: " + progress.Current);
        
        ImGui.End();
    }

    public IEnumerator Load()
    {
        if (loaded)
            yield break;
        
        Stopwatch sw = new();
        var cacheFile = CachePath;
        if (!cacheFile.Directory?.Exists ?? true)
            cacheFile.Directory!.Create();

        if (File.Exists(cacheFile.FullName))
        {
            LoadCached(cacheFile.FullName);
            yield break;
        }

        var tempFile = Path.GetTempFileName();
        var file = File.Open(tempFile, FileMode.Create);
        BinaryWriter binWriter = new BinaryWriter(file);
        
        sw.Start();
        progress = new GenerationProgress
        {
            Total = mapStore.Count * 64 * 64
        };
        
        foreach (var map in mapStore)
        {
            progress.Current = map.Name + " (" + map.Id + ")";

            var fullName = gameFiles.Wdt(map.Directory);
            var wdtBytesTask = gameFiles.ReadFile(fullName);
            yield return wdtBytesTask;

            if (wdtBytesTask.IsFaulted)
            {
                Console.WriteLine("Couldn't load file: " + fullName);
                progress.Done += 64 * 64;
                continue;
            }
            
            using var wdtBytes = wdtBytesTask.Result;

            if (wdtBytes == null)
            {
                progress.Done += 64 * 64;
                continue;
            }
            
            ChunkAreas?[,] areas = new ChunkAreas?[64, 64];
            areaTables[map.Id] = areas;
            
            var currentWdt = new FastWDTChunks(new MemoryBinaryReader(wdtBytes!));

            binWriter.Write(map.Id);
            var fileOffsetForCount = file.Position;
            int totalPresentChunks = 0;
            binWriter.Write((ushort)0);
            
            for (int y = 0; y < 64; ++y)
            {
                for (int x = 0; x < 64; ++x)
                {
                    progress.Done++;
                    if (currentWdt.Chunks[y][x])
                    {
                        var adtBytesTask = gameFiles.ReadFile(gameFiles.Adt(map.Directory, x, y));
                        yield return adtBytesTask;
                
                        using var adtBytes = adtBytesTask.Result;
                        if (adtBytes != null)
                        {
                            var adt = new FastAdtAreaTable(new MemoryBinaryReader(adtBytes));
                            totalPresentChunks++;
                            binWriter.Write((byte)y);
                            binWriter.Write((byte)x);
                            if (adt.AllAreasAreSame)
                            {
                                binWriter.Write((byte)0);
                                binWriter.Write(adt.AreaIds[0, 0]);
                            }
                            else
                            {
                                byte count = 0;
                                ushort? prevArea = null;
                                foreach (var area in adt.AreaIds)
                                {
                                    if (prevArea.HasValue && area != prevArea || count == 254)
                                    {
                                        binWriter.Write(count);
                                        binWriter.Write(prevArea!.Value);
                                        count = 0;
                                    }
                                    count++;
                                    prevArea = area;
                                }

                                if (count > 0)
                                {
                                    binWriter.Write(count);
                                    binWriter.Write(prevArea!.Value);
                                }
                            }

                            if (adt.AllAreasAreSame)
                                areas[y, x] = new ChunkAreas() { OneAreaForAll = adt.AreaIds[0, 0] };
                            else
                                areas[y, x] = new ChunkAreas() { Areas = adt.AreaIds };
                        }
                    }   
                }
            }

            var offset = file.Position;
            file.Position = fileOffsetForCount;
            binWriter.Write((ushort)totalPresentChunks);
            file.Position = offset;
        }
        Console.WriteLine("Loaded all areas in " + sw.Elapsed.TotalSeconds + "s ");

        file.Dispose();
        binWriter.Dispose();
        
        if (cacheFile.Exists)
            cacheFile.Delete();
        File.Move(tempFile, cacheFile.FullName);
        progress = null;
        
        loaded = true;
    }

    private void LoadCached(string fullName)
    {
        using var file = File.Open(fullName, FileMode.Open, FileAccess.Read);
        using BinaryReader br = new(file);
        var fileLength = file.Length;
        while (file.Position < fileLength)
        {
            var mapId = br.ReadInt32();
            var chunksCount = br.ReadUInt16();

            ChunkAreas?[,] mapAreaIds = new ChunkAreas?[64, 64];
            areaTables[mapId] = mapAreaIds;
            
            for (int i = 0; i < chunksCount; ++i)
            {
                var y = br.ReadByte();
                var x = br.ReadByte();
                
                var countOfAreas = br.ReadByte();
                if (countOfAreas == 0) // allAreasTheSame
                {
                    var areaId = br.ReadUInt16();
                    mapAreaIds[y, x] = new ChunkAreas(){OneAreaForAll = areaId};
                }
                else
                {
                    ushort[,] areaIds = new ushort[16, 16];
                    int a = 0;
                    int b = 0;
                    int readAreas = 0;
                    while (true)
                    {
                        var areaId = br.ReadUInt16();
                        for (int j = 0; j < countOfAreas; ++j)
                        {
                            areaIds[a, b] = areaId;
                            b++;
                            if (b == 16)
                            {
                                a++;
                                b = 0;
                            }
                        }
                        readAreas += countOfAreas;
                        if (readAreas == 16 * 16)
                            break;
                        countOfAreas = br.ReadByte();
                    }

                    mapAreaIds[y, x] = new ChunkAreas(){Areas = areaIds};
                }
            }
        }
    }
}