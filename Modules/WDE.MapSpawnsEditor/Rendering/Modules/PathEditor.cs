using System.Collections;
using ImGuiNET;
using TheEngine.Interfaces;
using WDE.Common.Database;
using WDE.MapRenderer.Managers;
using WDE.MapSpawnsEditor.ViewModels;

namespace WDE.MapSpawnsEditor.Rendering.Modules;

public class PathEditor : IMapSpawnModule, System.IDisposable
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IRenderManager renderManager;
    private readonly IGameContext gameContext;
    private List<UniversalWaypoint> points = new();
    private bool editing;
    private bool loadingPoints;
    private uint editingGuid;

    public PathEditor(IDatabaseProvider databaseProvider,
        IRenderManager renderManager,
        IGameContext gameContext)
    {
        this.databaseProvider = databaseProvider;
        this.renderManager = renderManager;
        this.gameContext = gameContext;
    }

    public void Dispose()
    {
    }
    
    public void Edit(CreatureSpawnInstance creature)
    {
        editingGuid = creature.Guid;
        editing = true;
        points.Clear();
        loadingPoints = true;
        gameContext.StartCoroutine(LoadPath(creature));
    }

    private IEnumerator LoadPath(CreatureSpawnInstance creature)
    {
        var task = databaseProvider.GetWaypointData(creature.Addon?.PathId ?? 0);
        yield return task;
        if (editingGuid == creature.Guid)
        {
            points.Clear();
            if (task.Result != null)
                points.AddRange(task.Result.Select(p => p.ToUniversal()));
            loadingPoints = false;
        }
    }

    public void RenderTransparent(float diff)
    {
        for (int i = 0; i < points.Count; ++i)
        {
            
        }
    }

    public void RenderGUI()
    {
        if (!editing)
            return;

        ImGui.Begin("Path editor");

        if (loadingPoints)
        {
            ImGui.Text("Loading path...");
            ImGui.End();
            return;
        }

        if (ImGui.BeginTable("path", 5))
        {
            ImGui.TableSetupScrollFreeze(1, 1);
            ImGui.TableSetupColumn("#");
            ImGui.TableSetupColumn("X");
            ImGui.TableSetupColumn("Y");
            ImGui.TableSetupColumn("Z");
            ImGui.TableSetupColumn("O");
            ImGui.TableHeadersRow();

            for (int i = 0; i < points.Count; ++i)
            {
                var point = points[i];
                ImGui.TableNextRow();

                ImGui.Selectable("#" + i, false, ImGuiSelectableFlags.SpanAllColumns);
                
                ImGui.TableNextColumn();
                ImGui.Text((i+1).ToString());
                ImGui.TableNextColumn();
                ImGui.Text(point.X.ToString("#.##"));
                ImGui.TableNextColumn();
                ImGui.Text(point.Y.ToString("#.##"));
                ImGui.TableNextColumn();
                ImGui.Text(point.Z.ToString("#.##"));
                ImGui.TableNextColumn();
                if (point.Orientation.HasValue)
                    ImGui.Text(point.Orientation.Value.ToString("#.##"));
            }
            
            ImGui.EndTable();
        }

        ImGui.End();
    }
}