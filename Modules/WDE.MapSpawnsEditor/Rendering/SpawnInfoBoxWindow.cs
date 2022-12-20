using ImGuiNET;
using TheMaths;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.Rendering.Modules;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.MapSpawnsEditor.Rendering;

internal class SpawnInfoBoxWindow : IMapSpawnModule
{
    private readonly ISpawnSelectionService selectionService;
    private readonly SpawnGroupTool spawnGroupTool;
    private readonly GenericExternalEdits externalEdits;
    private readonly IMainThread mainThread;

    public SpawnInfoBoxWindow(ISpawnSelectionService selectionService,
        SpawnGroupTool spawnGroupTool,
        GenericExternalEdits externalEdits,
        IMainThread mainThread)
    {
        this.selectionService = selectionService;
        this.spawnGroupTool = spawnGroupTool;
        this.externalEdits = externalEdits;
        this.mainThread = mainThread;
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
            ImGui.SameLine();
            ImGui.Text("(" + creature.CreatureTemplate.Entry + ")");
        }
        else if (go != null)
        {
            ImGui.TextColored(new Vector4(1, 0.8f, 0.2f, 1f), go.GameObjectTemplate.Name);
            ImGui.SameLine();
            ImGui.Text("(" + go.GameObjectTemplate.Entry + ")");
        }

        ImGui.BeginTable("spawn_info_box", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text("Spawn id");
        ImGui.TableNextColumn();
        ImGui.Text(selectedSpawn.Guid.ToString());

        ImGui.TableNextRow();
        var position = selectedSpawn.WorldObject!.Position;
        ImGui.TableNextColumn();
        ImGui.Text("Position"); ImGui.TableNextColumn();
        ImGui.Text($"({position.X}, {position.Y}, {position.Z})");

        if (creature != null)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Orientation"); ImGui.TableNextColumn();
            ImGui.Text(creature.Creature!.Orientation.ToString());
        }
        else if (go != null)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var rot = go.GameObject!.Rotation.ToEulerDeg();
            ImGui.Text("Rotation"); ImGui.TableNextColumn();
            ImGui.Text( $"Z: {rot.X:0.0}, Y: {rot.Y:0.0} Z: {rot.Z:0.0}");
        }

        if (creature != null)
        {
            {
                var hasCpp = !string.IsNullOrEmpty(creature.CreatureTemplate.ScriptName);
                var hasAi = !string.IsNullOrEmpty(creature.CreatureTemplate.AIName);
                
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Script"); ImGui.TableNextColumn();
                if (hasCpp)
                    ImGui.Text(creature.CreatureTemplate.ScriptName + " (C++)");
                else if (hasAi)
                    ImGui.Text(creature.CreatureTemplate.AIName);
                else
                    ImGui.Text("-");

                if (ImGui.Button(hasAi ? "Edit script" : (hasCpp ? "Add script (will remove cpp script)" : "Add script")))
                {
                    Execute(selectedSpawn, externalEdits.OpenScript);
                }

                ImGui.SameLine();
                if (ImGui.Button("Open spawn script"))
                {
                    Execute(selectedSpawn, externalEdits.OpenSpawnScript);
                }
            }
            
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Spawn group"); ImGui.TableNextColumn();
                if (creature.SpawnGroup == null)
                    ImGui.Text("-");
                else
                    ImGui.Text(creature.SpawnGroup.Name);
                
                if (selectedSpawn.SpawnGroup == null)
                {
                    if (ImGui.Button("Create group"))
                        Execute(selectedSpawn, spawnGroupTool.CreateAndAssignSpawnGroup);   
                    
                    ImGui.SameLine();
                    if (spawnGroupTool.LastSpawnGroup == null)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("Join last group");
                        ImGui.EndDisabled();
                    }
                    else
                    {
                        if (ImGui.Button("Join " + spawnGroupTool.LastSpawnGroup.Name.TrimToLength(30)))
                            Execute(selectedSpawn, spawnGroupTool.AssignSpawnGroup);
                    }
                }
                else
                {
                    if (ImGui.Button("Leave group"))
                        Execute(selectedSpawn, spawnGroupTool.LeaveSpawnGroup);
                    ImGui.SameLine();
                    if (ImGui.Button("Edit group"))
                        Execute(selectedSpawn, spawnGroupTool.EditSpawnGroup);
                    ImGui.SameLine();
                    if (ImGui.Button("Copy group"))
                        Execute(selectedSpawn, spawnGroupTool.MarkGroupAsLast);
                }
            }
        }
        
        ImGui.EndTable();

        ImGui.End();
    }

    private void Execute(Func<Task> fun)
    {
        mainThread.Dispatch(fun);
    }
    
    private void Execute(SpawnInstance spawn, Func<SpawnInstance, Task> fun)
    {
        mainThread.Dispatch(() => fun(spawn));
    }
    
    private void Execute(SpawnInstance spawn, Action<SpawnInstance> fun)
    {
        mainThread.Dispatch(() => fun(spawn));
    }
}