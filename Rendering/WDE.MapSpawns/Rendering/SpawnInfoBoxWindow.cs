using ImGuiNET;
using TheMaths;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering;

public class SpawnInfoBoxWindow
{
    private readonly ISpawnSelectionService selectionService;

    public SpawnInfoBoxWindow(ISpawnSelectionService selectionService)
    {
        this.selectionService = selectionService;
    }
    
    public void RenderGUI()
    {
        if (!(selectionService.SelectedSpawn.Value is { } selectedSpawn) || !selectedSpawn.IsSpawned)
            return;

        CreatureSpawnInstance? creature = selectedSpawn as CreatureSpawnInstance;
        GameObjectSpawnInstance? go = selectedSpawn as GameObjectSpawnInstance;

        ImGui.Begin("Selected spawn");
        
        if (creature != null)
        {
            ImGui.TextColored(new Vector4(1, 0.8f, 0.2f, 1f), creature.CreatureTemplate.Name);
            ImGui.LabelText("Entry", creature.CreatureTemplate.Entry.ToString());
            ImGui.LabelText("Spawn id", selectedSpawn.Header);
        }
        else if (go != null)
        {
            ImGui.TextColored(new Vector4(1, 0.8f, 0.2f, 1f), go.GameObjectTemplate.Name);
            ImGui.LabelText("Entry", go.GameObjectTemplate.Entry.ToString());
            ImGui.LabelText("Spawn id", selectedSpawn.Header);
        }

        var position = selectedSpawn.WorldObject!.Position;
        ImGui.LabelText("Position", $"({position.X}, {position.Y}, {position.Z})");
        
        if (creature != null)
            ImGui.LabelText("Orientation", creature.Creature!.Orientation.ToString());
        else if (go != null)
        {
            var rot = go.GameObject!.Rotation;
            var euler = rot.ToEulerDeg();
            ImGui.LabelText("Orientation", go.GameObject!.Orientation.ToString());
            ImGui.Text("Rotation (Euler/Real)");
            ImGui.LabelText("X (Euler/Real)", $"{euler.X:0.0} / {rot.X:0.0}");
            ImGui.LabelText("Y (Euler/Real)", $"{euler.Y:0.0} / {rot.Y:0.0}");
            ImGui.LabelText("Z (Euler/Real)", $"{euler.Z:0.0} / {rot.Z:0.0}");
            ImGui.LabelText("W (Euler/Real)", $"{rot.W:0.0}");
        }

        ImGui.End();
    }
}