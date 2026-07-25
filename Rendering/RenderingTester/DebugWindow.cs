using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Hexa.NET.ImGui;
using Tedd;
using TheEngine.Utils;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Utils;
using WDE.MpqReader.DBC;

namespace RenderingTester;

public unsafe class DebugWindow : IGameModule
{
    private readonly MapStore maps;
    private readonly ChunkManager chunkManager;
    private readonly IGameContext gameContext;
    private readonly IGameProperties gameProperties;
    public object? ViewModel => null;
    private int selectedMap = 0;
    private byte** mapNamesArray;
    private byte* mapNamesFlatStore;
    private int[] allMapIds;
    private bool renderTerrain = true;

    public DebugWindow(MapStore maps,
        ChunkManager chunkManager,
        IGameContext gameContext,
        IGameProperties gameProperties)
    {
        this.maps = maps;
        this.chunkManager = chunkManager;
        this.gameContext = gameContext;
        this.gameProperties = gameProperties;
        allMapIds = new int[maps.Count];
        int index = 0;
        foreach (var map in maps)
        {
            allMapIds[index++] = map->Id;
        }
    }
    
    public void Dispose()
    {
        NativeMemory.Free(mapNamesArray);
        NativeMemory.Free(mapNamesFlatStore);
    }

    public static int GetUtf8ByteCountMath(int value)
    {
        if (value == 0) return 1;
        if (value == int.MinValue) return 11; // "-2147483648"

        int bytes = 0;
        if (value < 0)
        {
            bytes++; // For the negative sign '-'
            value = -value;
        }

        bytes += (int)Math.Floor(Math.Log10(value)) + 1;

        return bytes;
    }

    public void Initialize()
    {
        nuint totalLength = 0;
        foreach (var map in maps)
        {
            totalLength += (nuint)GetUtf8ByteCountMath(map->Id);
            totalLength += 3; // " - "u8
            totalLength += (nuint)map->Name.Length;
            totalLength += 1; // zero byte
        }

        mapNamesFlatStore = (byte*)NativeMemory.Alloc(totalLength);
        mapNamesArray = (byte**)NativeMemory.Alloc((nuint)(maps.Count * Unsafe.SizeOf<nuint>()));
        var nativeMemoryAsSpan = new Span<byte>(mapNamesFlatStore, (int)totalLength);
        var offset = 0;
        var index = 0;
        foreach (var map in maps)
        {
            mapNamesArray[index++] = (byte*)mapNamesFlatStore + offset;
            offset += nativeMemoryAsSpan.MoveWriteAsDecimal(map->Id);
            offset += nativeMemoryAsSpan.MoveWrite(" - "u8);
            offset += nativeMemoryAsSpan.MoveWrite(map->Name.AsSpan());
            offset += nativeMemoryAsSpan.MoveWrite((byte)0);
        }
    }

    public void Update(float delta)
    {
    }

    public void RenderGUI()
    {
        ImGui.Begin("Debug");

        selectedMap = allMapIds.IndexOf(gameContext.CurrentMapId);
        if (ImGui.Combo("Map", ref selectedMap, mapNamesArray, maps.Count))
            gameContext.SetMap(allMapIds[selectedMap]);

        if (ImGui.Checkbox("Render terrain", ref renderTerrain))
            chunkManager.RenderTerrain = renderTerrain;

        float viewDistance = (float)gameProperties.ViewDistanceModifier;
        if (ImGui.SliderFloat("View Distance", ref viewDistance, 1, 32))
            gameProperties.ViewDistanceModifier = viewDistance;

        bool pausedTime = gameProperties.DisableTimeFlow;
        if (ImGui.Checkbox("Pause time", ref pausedTime))
            gameProperties.DisableTimeFlow = pausedTime;

        var minutes = gameProperties.CurrentTime.TotalMinutes;
        if (ImGui.SliderInt("Current time", ref minutes, 0, 1439))
            gameProperties.CurrentTime = Time.FromMinutes(minutes);
        
        bool renderGui = gameProperties.RenderGui;
        if (ImGui.Checkbox("Render GUI", ref renderGui))
            gameProperties.RenderGui = renderGui;

        float dynamicResolution = gameProperties.DynamicResolution;
        if (ImGui.SliderFloat("Dynamic scale", ref dynamicResolution, 0.1f, 1))
            gameProperties.DynamicResolution = dynamicResolution;

        ImGui.End();
    }
}