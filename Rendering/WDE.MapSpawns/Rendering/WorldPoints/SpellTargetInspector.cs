using TheEngine;
using System.Numerics;
using Hexa.NET.ImGui;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.WorldPoints;

namespace WDE.MapSpawns.Rendering.WorldPoints;

/// <summary>
/// The Spell-target tool's inspector: arm a spell id and click the world to create its destination,
/// pick/select markers to move them (gizmo or fields), browse all rows (cross-map rows fly the
/// camera over), delete with confirm. Spell ids resolve to names snapshotted from the app's spell
/// store; an id with no name gets a warning (the core rejects rows for nonexistent spells).
/// </summary>
public sealed class SpellTargetInspector : IInspectorSection
{
    private readonly ISpellTargetEditorService service;
    private readonly SpellTargetEditorModule module;
    private readonly IGameContext gameContext;
    private readonly EntryPickerService entryPicker;

    private int newSpellId;
    private string filter = "";

    public SpellTargetInspector(ISpellTargetEditorService service, SpellTargetEditorModule module,
        IGameContext gameContext, EntryPickerService entryPicker)
    {
        this.service = service;
        this.module = module;
        this.gameContext = gameContext;
        this.entryPicker = entryPicker;
    }

    public string Title => "Spell targets";

    public bool IsDirty => service.IsSupported && service.AnyDirty;

    public Func<Task>? SaveSelf => IsDirty ? service.Save : null;

    public Func<Task>? RevertSelf => IsDirty ? RevertAll : null;

    private Task RevertAll()
    {
        module.Reload();
        return Task.CompletedTask;
    }

    public string? Hints
    {
        get
        {
            if (!service.IsSupported)
                return "The current core has no spell_target_position table";
            if (module.DragHint is { } dragHint)
                return dragHint;
            if (module.PlacementArmed)
                return $"Click the world to place the destination of {module.DescribeSpell(module.PendingSpellId)}";
            if (module.SelectedKey != null)
                return "G grab · G again snap to ground · R rotate (landing facing) · Del: delete · Esc cancel · click empty ground: deselect";
            return "Click a marker to edit it · enter a spell id and place its destination";
        }
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\nspell_target_position table."u8);
            return;
        }

        if (module.SelectedKey is { } key && service.Positions.TryGetValue(key, out var row))
            DrawEditor(row);
        else
            DrawOverview();
    }

    private void DrawOverview()
    {
        // add flow: spell id -> name preview -> arm placement
        ImGui.SetNextItemWidth(110);
        ImGui.InputInt("##newspell"u8, ref newSpellId, 0, 0);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Spell id (the spell must use target TARGET_LOCATION_DATABASE / 17)"u8);
        entryPicker.PickButton("pickspell", "SpellParameter", "Pick a spell from the list",
            newSpellId, picked => newSpellId = (int)picked);
        ImGui.SameLine();

        bool exists = newSpellId > 0 && service.Positions.ContainsKey((uint)newSpellId);
        if (module.PlacementArmed)
        {
            if (ImGui.Button($"{Lucide.X} Cancel placement"))
            {
                module.PlacementArmed = false;
                module.PendingSpellId = 0;
            }
        }
        else
        {
            ImGui.BeginDisabled(newSpellId <= 0 || exists);
            if (ImGui.Button("Place destination (click world)"u8))
            {
                module.PendingSpellId = (uint)newSpellId;
                module.PlacementArmed = true;
            }
            ImGui.EndDisabled();
        }

        if (newSpellId > 0)
        {
            var name = service.GetSpellName((uint)newSpellId);
            if (exists)
                EditorWidgets.WrappedHint("This spell already has a destination - select its marker."u8);
            else if (name != null)
                ImGui.TextDisabled(name);
            else
                ImGui.TextColored(EditorTheme.Warning, "Unknown spell id"u8);
        }

        ImGui.SeparatorText("Destinations on this map"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Destinations on other maps: open their map first"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##filter"u8, "filter by spell name or id"u8, ref filter, 100);

        if (ImGui.BeginChild("##rows"u8))
        {
            int total = 0, shown = 0;
            foreach (var row in service.Positions.Values
                         .Where(r => r.Map == (uint)module.CurrentMapId)
                         .OrderBy(r => r.SpellId))
            {
                total++;
                string label = module.DescribeSpell(row.SpellId);
                if (filter.Length > 0 && !label.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
                shown++;

                if (ImGui.Selectable(label))
                    module.SelectedKey = row.SpellId;
                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        module.FlyTo(row.Map, row.Position);
                    ImGui.SetTooltip("Click: edit · double-click: fly camera to it"u8);
                }
            }
            if (total == 0)
                ImGui.TextDisabled("No spell destinations on this map."u8);
            else if (shown == 0)
                ImGui.TextDisabled($"No destinations match \"{filter}\"");
        }
        ImGui.EndChild();
    }

    private void DrawEditor(SpellTargetData row)
    {
        if (EditorWidgets.BackRow("All destinations", "Back to the destination list (a marker click reopens the editor)"))
        {
            module.SelectedKey = null;
            return;
        }

        EditorWidgets.WrappedLabel(module.DescribeSpell(row.SpellId));
        if (service.GetSpellName(row.SpellId) == null)
            EditorWidgets.WrappedWarning("Unknown spell id - the core skips this row"u8);
        var dbc = gameContext.DbcManager;
        ImGui.TextDisabled(WorldPointNames.MapName(dbc, (int)row.Map));

        bool changed = false;

        var pos = new Vector3(row.Position.X, row.Position.Y, row.Position.Z);
        EditorWidgets.FitNextItem("Position"u8);
        if (ImGui.InputFloat3("Position", ref pos))
        {
            row.Position = new Vector3(pos.X, pos.Y, pos.Z);
            changed = true;
        }

        float orientation = row.Orientation;
        EditorWidgets.FitNextItem("Facing"u8);
        if (ImGui.SliderFloat("Facing"u8, ref orientation, 0f, MathF.Tau, "%.3f rad"u8))
        {
            row.Orientation = orientation;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"The teleported unit lands facing this way ({orientation * 180f / MathF.PI:0.#}°)\nCtrl+click to type an exact value");

        if (ImGui.SmallButton($"{Lucide.ArrowDownToLine} Snap to ground"))
        {
            row.Position = module.SnapToGround(row.Position);
            changed = true;
        }
        ImGui.SameLine();
        if (EditorWidgets.FlyToButton("destination"))
            module.FlyTo(row.Map, row.Position);

        ImGui.Separator();
        DrawDelete(row);

        if (changed)
            service.NotifyChanged(row.SpellId);
    }

    private void DrawDelete(SpellTargetData row)
    {
        if (ImGui.Button($"{Lucide.Trash2} Delete destination...", new Vector2(-1, 0)))
            ImGui.OpenPopup("Delete spell target"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Deletes the spell_target_position row. Applied on Save.\nThe spell will fail to resolve its destination!"u8);

        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Delete spell target", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextUnformatted($"Delete the destination of {module.DescribeSpell(row.SpellId)}?");
        ImGui.TextDisabled("The database row is removed when you Save."u8);
        ImGui.Separator();
        if (ImGui.Button("Delete"u8, new Vector2(120, 0)))
        {
            service.DeletePosition(row.SpellId);
            module.SelectedKey = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"u8, new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }
}
