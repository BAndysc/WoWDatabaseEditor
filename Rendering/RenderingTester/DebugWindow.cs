using ImGuiNET;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MpqReader.DBC;

namespace RenderingTester;

public class DebugWindow : IGameModule
{
    private readonly MapStore maps;
    private readonly ChunkManager chunkManager;
    private readonly IGameContext gameContext;
    private readonly IGameProperties gameProperties;
    public object? ViewModel => null;
    private int selectedMap = 0;
    private string[] allMapNames;
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
        allMapNames = maps.Select(x => x.Id + " - " + x.Name).ToArray();
        allMapIds = maps.Select(x => x.Id).ToArray();
    }
    
    public void Dispose()
    {
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
    }

    public void RenderGUI()
    {
        ImGui.Begin("Debug");

        selectedMap = allMapIds.IndexOf(gameContext.CurrentMap.Id);
        
        if (ImGui.Combo("Map", ref selectedMap, allMapNames, allMapNames.Length))
            gameContext.SetMap(allMapIds[selectedMap]);

        if (ImGui.Checkbox("Render terrain", ref renderTerrain))
            chunkManager.RenderTerrain = renderTerrain;

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