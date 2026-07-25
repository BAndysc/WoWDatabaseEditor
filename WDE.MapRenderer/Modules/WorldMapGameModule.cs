using Hexa.NET.ImGui;
using TheEngine;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Resources;
using TheMaths;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;
using Constants = WDE.MapRenderer.StaticData.Constants;
using IInputManager = TheEngine.Interfaces.IInputManager;
using ITextureManager = TheEngine.Interfaces.ITextureManager;

namespace WDE.MapRenderer.Modules;

public class WorldMapGameModule : IGameModule
{
    private const float MinTileSize = 4;
    private const float MaxTileSize = 512;
    private const int MaxConcurrentTileLoads = 8;

    private const uint BackgroundColor = 0xFF101010;
    private const uint WaterColor = 0xFF804020;
    private const uint TerrainColor = 0xFF2E4A3D;
    private const uint LoadingColor = 0xFF303030;
    private const uint HoverColor = 0x40FFFFFF;
    private const uint CameraColor = 0xFF0000FF;
    private const uint PhantomCameraColor = 0x800000FF;

    private readonly IInputManager inputManager;
    private readonly IGameContext gameContext;
    private readonly CameraManager cameraManager;
    private readonly WorldManager worldManager;
    private readonly WorldMapAreaStore worldMapAreaStore;
    private readonly AreaTableStore areaTableStore;
    private readonly IGameFiles gameFiles;
    private readonly ITextureManager textureManager;
    private readonly Engine engine;

    public object? ViewModel => null;

    private bool mapOpened;
    private bool centerOnCameraRequest;
    private bool draggedSinceActivated;
    private float tileSize = 16;

    // parsed md5translate.trs: map directory -> (tileX, tileY) -> minimap blp (relative to textures\minimap\)
    private Dictionary<string, Dictionary<(int x, int y), string>>? trs;
    private Task? trsLoadTask;

    private int currentTilesMapId = -1;
    private Dictionary<(int x, int y), string>? currentMapTiles;
    private string? currentMapDirectory;

    // value null = load attempted, no usable minimap (fall back to a colored square)
    private readonly Dictionary<(int x, int y), ITexture?> tileTextures = new();
    private readonly HashSet<(int x, int y)> tilesBeingLoaded = new();
    private int loadGeneration;

    public WorldMapGameModule(IInputManager inputManager,
        IGameContext gameContext,
        CameraManager cameraManager,
        WorldManager worldManager,
        WorldMapAreaStore worldMapAreaStore,
        AreaTableStore areaTableStore,
        IGameFiles gameFiles,
        ITextureManager textureManager,
        Engine engine)
    {
        this.inputManager = inputManager;
        this.gameContext = gameContext;
        this.cameraManager = cameraManager;
        this.worldManager = worldManager;
        this.worldMapAreaStore = worldMapAreaStore;
        this.areaTableStore = areaTableStore;
        this.gameFiles = gameFiles;
        this.textureManager = textureManager;
        this.engine = engine;
    }

    public void Initialize()
    {
        gameContext.ChangedMap += OnChangedMap;
    }

    public void Update(float delta)
    {
        if (inputManager.Keyboard.JustPressed(Key.M))
        {
            mapOpened = !mapOpened;
            if (mapOpened)
            {
                centerOnCameraRequest = true;
                trsLoadTask ??= LoadTrsAsync();
            }
        }
    }

    public unsafe void RenderGUI()
    {
        if (!mapOpened)
            return;

        RefreshCurrentMapTiles();

        ImGui.SetNextWindowSize(new Vector2(700, 740), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Map"u8, ref mapOpened))
        {
            ImGui.SetNextItemWidth(180);
            ImGui.SliderFloat("##zoom"u8, ref tileSize, MinTileSize, MaxTileSize, "%.0f px / tile"u8, ImGuiSliderFlags.Logarithmic);
            ImGui.SameLine();
            if (ImGui.Button("Center on camera"u8))
                centerOnCameraRequest = true;
            if (trsLoadTask is { IsCompleted: false })
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(loading minimaps...)"u8);
            }

            bool hoverValid = false;
            Vector2 hoverWow = default;
            int hoverTileX = 0, hoverTileY = 0;

            if (ImGui.BeginChild("##worldmap"u8, new Vector2(0, -ImGui.GetFrameHeightWithSpacing()),
                    ImGuiChildFlags.None,
                    ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse))
            {
                var io = ImGui.GetIO();
                var drawList = ImGui.GetWindowDrawList();
                var canvasSize = new Vector2(Constants.Blocks * tileSize, Constants.Blocks * tileSize);
                var viewSize = ImGui.GetWindowSize();
                // when zoomed out below the view size, center the canvas instead of anchoring it top-left
                var centerPadding = new Vector2(MathF.Max(0, (viewSize.X - canvasSize.X) * 0.5f),
                    MathF.Max(0, (viewSize.Y - canvasSize.Y) * 0.5f));
                ImGui.SetCursorPos(ImGui.GetCursorPos() + centerPadding);
                var origin = ImGui.GetCursorScreenPos();
                ImGui.InvisibleButton("##canvas"u8, canvasSize);
                bool hovered = ImGui.IsItemHovered();
                bool active = ImGui.IsItemActive();

                if (centerOnCameraRequest)
                {
                    var camCanvas = WowToCanvas(cameraManager.Position.XY(), canvasSize);
                    ImGui.SetScrollX(camCanvas.X - viewSize.X * 0.5f);
                    ImGui.SetScrollY(camCanvas.Y - viewSize.Y * 0.5f);
                    centerOnCameraRequest = false;
                }

                drawList.AddRectFilled(origin, origin + canvasSize, BackgroundColor);

                var scroll = new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
                int minX = Math.Max(0, (int)(scroll.X / tileSize));
                int minY = Math.Max(0, (int)(scroll.Y / tileSize));
                int maxX = Math.Min(Constants.Blocks - 1, (int)((scroll.X + viewSize.X) / tileSize));
                int maxY = Math.Min(Constants.Blocks - 1, (int)((scroll.Y + viewSize.Y) / tileSize));

                for (int y = minY; y <= maxY; ++y)
                {
                    for (int x = minX; x <= maxX; ++x)
                    {
                        var tileMin = origin + new Vector2(x, y) * tileSize;
                        var tileMax = tileMin + new Vector2(tileSize, tileSize);

                        var minimapFile = MinimapTilePath(x, y);

                        ITexture? texture = null;
                        bool loadFinished = minimapFile != null && tileTextures.TryGetValue((x, y), out texture);
                        if (minimapFile != null && !loadFinished)
                            RequestTileLoad(x, y, minimapFile);

                        var hasAdt = worldManager.IsChunkPresent(x, y, out var adtType);
                        if (texture != null)
                            drawList.AddImage(new ImTextureRef(null, texture.Handle.ToRawIntPtr()), tileMin, tileMax);
                        else if (minimapFile != null && !loadFinished)
                            drawList.AddRectFilled(tileMin, tileMax, LoadingColor);
                        else if (hasAdt)
                            drawList.AddRectFilled(tileMin, tileMax, adtType == AdtChunkType.AllWater ? WaterColor : TerrainColor);
                    }
                }

                // camera position marker
                var cameraCanvas = origin + WowToCanvas(cameraManager.Position.XY(), canvasSize);
                drawList.AddCircleFilled(cameraCanvas, 5, CameraColor);
                drawList.AddCircle(cameraCanvas, 5, 0xFF000000);

                if (hovered)
                {
                    var mouseCanvas = io.MousePos - origin;
                    hoverTileX = (int)(mouseCanvas.X / tileSize);
                    hoverTileY = (int)(mouseCanvas.Y / tileSize);
                    if (hoverTileX >= 0 && hoverTileX < Constants.Blocks && hoverTileY >= 0 && hoverTileY < Constants.Blocks)
                    {
                        hoverValid = true;
                        hoverWow = CanvasToWow(mouseCanvas, canvasSize);
                        var tileMin = origin + new Vector2(hoverTileX, hoverTileY) * tileSize;
                        drawList.AddRectFilled(tileMin, tileMin + new Vector2(tileSize, tileSize), HoverColor);
                        // phantom marker: where the camera would land on click
                        drawList.AddCircleFilled(io.MousePos, 5, PhantomCameraColor);
                        drawList.AddCircle(io.MousePos, 5, 0x80000000);
                    }

                    // wheel zooms towards the cursor
                    if (io.MouseWheel != 0)
                    {
                        var oldTileSize = tileSize;
                        tileSize = Math.Clamp(tileSize * MathF.Pow(1.2f, io.MouseWheel), MinTileSize, MaxTileSize);
                        var ratio = tileSize / oldTileSize;
                        ImGui.SetScrollX(scroll.X + mouseCanvas.X * (ratio - 1));
                        ImGui.SetScrollY(scroll.Y + mouseCanvas.Y * (ratio - 1));
                    }
                }

                if (ImGui.IsItemActivated())
                    draggedSinceActivated = false;

                if (active && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4))
                {
                    draggedSinceActivated = true;
                    ImGui.SetScrollX(ImGui.GetScrollX() - io.MouseDelta.X);
                    ImGui.SetScrollY(ImGui.GetScrollY() - io.MouseDelta.Y);
                }

                if (ImGui.IsItemDeactivated() && !draggedSinceActivated && hoverValid)
                {
                    var hasTarget = worldManager.IsChunkPresent(hoverTileX, hoverTileY, out _) ||
                                    (tileTextures.TryGetValue((hoverTileX, hoverTileY), out var hoverTex) && hoverTex != null);
                    if (hasTarget)
                        cameraManager.Relocate(new Vector3(hoverWow, 300));
                }
            }
            ImGui.EndChild();

            if (hoverValid)
            {
                ImGui.Text($"{hoverWow.X:0.0} {hoverWow.Y:0.0}   tile {hoverTileX}, {hoverTileY}");
                var hoveredZone = worldMapAreaStore.FindClosest(gameContext.CurrentMapId, hoverWow.X, hoverWow.Y);
                if (hoveredZone != null && areaTableStore.TryGetValue(hoveredZone->ZoneId, out var zone))
                {
                    ImGui.SameLine();
                    ImGui.Text(zone->Name.AsSpan());
                }
            }
            else
                ImGui.TextDisabled("scroll = zoom, drag = pan, click = teleport"u8);
        }
        ImGui.End();
    }

    private unsafe void RefreshCurrentMapTiles()
    {
        if (trs == null)
            return;
        var mapId = gameContext.CurrentMapId;
        if (mapId == currentTilesMapId)
            return;
        currentTilesMapId = mapId;
        currentMapTiles = null;
        currentMapDirectory = null;
        if (gameContext.CurrentMap != null)
        {
            currentMapDirectory = gameContext.CurrentMap->Directory.ToString();
            if (trs.TryGetValue(currentMapDirectory, out var tiles))
                currentMapTiles = tiles;
        }
    }

    // Two minimap storage schemes: pre-Cata clients dedup all-water tiles via textures\minimap\md5translate.trs
    // (md5-named files), Cata+ ships plain World\Minimaps\<mapdir>\mapXX_YY.blp files and has no trs at all.
    private string? MinimapTilePath(int x, int y)
    {
        if (currentMapTiles != null)
            return currentMapTiles.TryGetValue((x, y), out var md5Name) ? "textures\\minimap\\" + md5Name : null;
        if (trs != null && currentMapDirectory != null)
            return $"World\\Minimaps\\{currentMapDirectory}\\map{x:00}_{y:00}.blp";
        return null;
    }

    private void OnChangedMap(int mapId)
    {
        ClearTileCache();
    }

    private void ClearTileCache()
    {
        loadGeneration++;
        foreach (var texture in tileTextures.Values)
        {
            if (texture != null)
                textureManager.DisposeTexture(texture);
        }
        tileTextures.Clear();
        tilesBeingLoaded.Clear();
        currentTilesMapId = -1;
        currentMapTiles = null;
        currentMapDirectory = null;
    }

    private async Task LoadTrsAsync()
    {
        var result = new Dictionary<string, Dictionary<(int x, int y), string>>(StringComparer.OrdinalIgnoreCase);
        var bytes = await gameFiles.ReadFile("textures\\Minimap\\md5translate.trs", true);
        if (bytes != null)
        {
            await engine.EnterThreadPool;
            try
            {
                ParseTrs(bytes, result);
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't parse md5translate.trs: " + e.Message);
            }
            finally
            {
                bytes.Dispose();
            }
            await engine.EnterGameLoop;
        }
        trs = result;
        currentTilesMapId = -1;
    }

    private static void ParseTrs(PooledArray<byte> bytes, Dictionary<string, Dictionary<(int x, int y), string>> result)
    {
        var text = System.Text.Encoding.ASCII.GetString(bytes.AsArray(), 0, bytes.Length);
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("dir:", StringComparison.OrdinalIgnoreCase))
                continue;
            var tab = line.IndexOf('\t');
            if (tab < 0)
                continue;
            // one side is "<mapdir>\map<x>_<y>.blp", the other the md5-named file
            // (older trs versions have the sides swapped)
            var tilePath = line[..tab].Trim();
            var md5Name = line[(tab + 1)..].Trim();
            if (!TryParseTilePath(tilePath, out var dir, out var tile))
            {
                if (!TryParseTilePath(md5Name, out dir, out tile))
                    continue;
                md5Name = tilePath;
            }
            if (!result.TryGetValue(dir, out var perMap))
                result[dir] = perMap = new();
            perMap[tile] = md5Name;
        }
    }

    private static bool TryParseTilePath(string path, out string dir, out (int x, int y) tile)
    {
        dir = "";
        tile = default;
        var slash = path.LastIndexOfAny(new[] { '\\', '/' });
        if (slash <= 0)
            return false;
        var name = path.AsSpan(slash + 1);
        if (!name.StartsWith("map", StringComparison.OrdinalIgnoreCase) ||
            !name.EndsWith(".blp", StringComparison.OrdinalIgnoreCase))
            return false;
        var coords = name[3..^4];
        var underscore = coords.IndexOf('_');
        if (underscore < 0)
            return false;
        if (!int.TryParse(coords[..underscore], out var x) || !int.TryParse(coords[(underscore + 1)..], out var y))
            return false;
        dir = path[..slash];
        tile = (x, y);
        return true;
    }

    private void RequestTileLoad(int x, int y, string file)
    {
        if (tilesBeingLoaded.Count >= MaxConcurrentTileLoads || tilesBeingLoaded.Contains((x, y)))
            return;
        tilesBeingLoaded.Add((x, y));
        LoadTileAsync(x, y, file, loadGeneration).ListenErrors();
    }

    private async Task LoadTileAsync(int x, int y, string file, int generation)
    {
        ITexture? texture = null;
        var bytes = await gameFiles.ReadFile(file, true);
        if (bytes != null)
        {
            await engine.EnterThreadPool;
            BLP? blp = null;
            try
            {
                blp = new BLP(bytes.AsArray(), 0, bytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Invalid minimap BLP {file}: {e.Message}");
            }
            finally
            {
                bytes.Dispose();
            }
            if (blp != null)
                texture = await textureManager.CreateTextureAsync(blp.Data, (int)blp.RealWidth, (int)blp.RealHeight,
                    blp.Header.Mips == BLP.MipmapLevelAndFlagType.MipsNone, FilteringMode.Linear, WrapMode.ClampToEdge);
            else
                await engine.EnterGameLoop; // resume on the game loop so the cache write below is thread-safe
        }

        if (generation == loadGeneration)
        {
            tilesBeingLoaded.Remove((x, y));
            tileTextures[(x, y)] = texture;
        }
        else if (texture != null)
            textureManager.DisposeTexture(texture);
    }

    private static Vector2 CanvasToWow(Vector2 canvasPos, Vector2 canvasSize)
    {
        var pct = new Vector2(1, 1) - canvasPos / canvasSize;
        return new Vector2(pct.Y * Constants.MapSize - Constants.MapSize / 2, pct.X * Constants.MapSize - Constants.MapSize / 2);
    }

    private static Vector2 WowToCanvas(Vector2 wow, Vector2 canvasSize)
    {
        var pct = new Vector2(wow.Y / Constants.MapSize + 0.5f, wow.X / Constants.MapSize + 0.5f);
        return (new Vector2(1, 1) - pct) * canvasSize;
    }

    public void Dispose()
    {
        gameContext.ChangedMap -= OnChangedMap;
        ClearTileCache();
    }
}
