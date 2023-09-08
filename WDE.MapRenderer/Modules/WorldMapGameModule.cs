using Avalonia.Input;
using ImGuiNET;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;
using IInputManager = TheEngine.Interfaces.IInputManager;

namespace WDE.MapRenderer.Modules;

public class WorldMapGameModule : IGameModule
{
    private readonly IInputManager inputManager;
    private readonly IGameContext gameContext;
    private readonly CameraManager cameraManager;
    private readonly WorldManager worldManager;
    private readonly WorldMapAreaStore worldMapAreaStore;
    private readonly AreaTableStore areaTableStore;

    public object? ViewModel => null;

    private bool mapOpened;
    
    public WorldMapGameModule(IInputManager inputManager,
        IGameContext gameContext,
        CameraManager cameraManager,
        WorldManager worldManager,
        WorldMapAreaStore worldMapAreaStore,
        AreaTableStore areaTableStore)
    {
        this.inputManager = inputManager;
        this.gameContext = gameContext;
        this.cameraManager = cameraManager;
        this.worldManager = worldManager;
        this.worldMapAreaStore = worldMapAreaStore;
        this.areaTableStore = areaTableStore;
    }
    
    public void Initialize()
    {
    }

    public void Update(float delta)
    {
        if (inputManager.Keyboard.JustPressed(Key.M))
            mapOpened = !mapOpened;
    }

    public void RenderGUI()
    {
        const int ButtonWidth = 8;
        if (mapOpened)
        {
            if (!ImGui.Begin("Map", ImGuiWindowFlags.AlwaysAutoResize))
                return;

            var contentPosition = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();
            var localSpaceCursorPosition = ImGui.GetIO().MousePos - contentPosition;
            var percentCursorPosition = new Vector2(1, 1) - localSpaceCursorPosition / (Constants.Blocks * ButtonWidth);
            var wowCursorPosition = new Vector2(percentCursorPosition.Y * Constants.MapSize - Constants.MapSize/2, percentCursorPosition.X * Constants.MapSize - Constants.MapSize / 2);
            
            var currentCameraPosition = cameraManager.Position;
            var pctCurrentCameraPosition = new Vector2(currentCameraPosition.Y / Constants.MapSize + 0.5f, currentCameraPosition.X / Constants.MapSize + 0.5f);
            var localSpaceCurrentCameraPosition = (new Vector2(1, 1) - pctCurrentCameraPosition) * Constants.Blocks * ButtonWidth;
            var globalSpaceCurrentCameraPosition = localSpaceCurrentCameraPosition + contentPosition;
            
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(0, 0));
            if (ImGui.BeginTable("worldmap", Constants.Blocks, ImGuiTableFlags.NoPadInnerX))
            {
                for (int y = 0; y < Constants.Blocks; ++y)
                {
                    ImGui.TableNextRow();

                    for (int x = 0; x < Constants.Blocks; ++x)
                    {
                        ImGui.TableNextColumn();
                        var hasAdt = worldManager.IsChunkPresent(x, y, out var adtType);
                        ImGui.BeginDisabled(!hasAdt);
                        var color = adtType == AdtChunkType.AllWater ? new Vector4(0, 0, 1, 1) : 
                                (adtType == AdtChunkType.None ? new Vector4(0, 0, 0, 1) : new Vector4(0.5f, 0.2f, 0.3f, 1));
                        ImGui.PushStyleColor(ImGuiCol.Button, color);
                        
                        ImGui.PushID(new IntPtr(x << 8 | y));
                        if (ImGui.Button("", new Vector2(ButtonWidth, ButtonWidth)))
                        {
                            cameraManager.Relocate(new Vector3(wowCursorPosition, 300));
                        }
                        ImGui.PopID();
                        
                        ImGui.PopStyleColor();
                        ImGui.EndDisabled();
                    }
                }
                ImGui.EndTable();
            }
            ImGui.PopStyleVar();
            
            ImGui.Text($"{wowCursorPosition.X:0.00} {wowCursorPosition.Y:0.00}");
            var hoveredZone = worldMapAreaStore.FindClosest(gameContext.CurrentMap.Id, wowCursorPosition.X, wowCursorPosition.Y);
            if (hoveredZone != null)
            {
                if (areaTableStore.TryGetValue(hoveredZone.ZoneId, out var zone))
                    ImGui.Text(zone.Name);
            }
            
            ImGui.GetWindowDrawList().AddCircleFilled(globalSpaceCurrentCameraPosition, 4, 0xFF0000FF);
            
            ImGui.End();
        }
    }
    
    public void Dispose()
    {
    }
}