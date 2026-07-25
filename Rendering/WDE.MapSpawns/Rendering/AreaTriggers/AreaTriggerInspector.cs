using Hexa.NET.ImGui;
using TheEngine;
using TheMaths;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.AreaTriggers;
using WDE.MapSpawns.Rendering.WorldPoints;
using WDE.QueryGenerators.Generators.AreaTriggers;

namespace WDE.MapSpawns.Rendering.AreaTriggers;

/// <summary>
/// The Area-trigger tool's inspector: browse the DBC triggers of the current map, and for the
/// selected one edit its database side tables - teleport destination (with the per-expansion
/// requirement columns), tavern rest flag, exploration quest and script binding. The trigger
/// shapes themselves are client data and cannot be edited.
/// </summary>
public sealed class AreaTriggerInspector : IInspectorSection
{
    private readonly IAreaTriggerEditorService service;
    private readonly AreaTriggerEditorModule module;
    private readonly IGameContext gameContext;
    private readonly EntryPickerService entryPicker;

    private string filter = "";

    public AreaTriggerInspector(IAreaTriggerEditorService service, AreaTriggerEditorModule module,
        IGameContext gameContext, EntryPickerService entryPicker)
    {
        this.service = service;
        this.module = module;
        this.gameContext = gameContext;
        this.entryPicker = entryPicker;
    }

    public string Title => "Area triggers";

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
                return "The current core has no areatrigger_teleport table";
            if (module.DragHint is { } dragHint)
                return dragHint;
            if (module.PlacementArmed)
                return "Click the world to place the teleport destination (on this map)";
            if (module.SelectedKey != null)
                return "G grab destination · R rotate (landing facing) · Del: delete teleport · click another trigger to switch";
            return "Click a trigger shape to edit its teleport/tavern/quest data";
        }
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\nareatrigger_teleport table."u8);
            return;
        }

        if (module.SelectedKey is { } key)
            DrawEditor(key);
        else
            DrawOverview();
    }

    private void DrawOverview()
    {
        ImGui.TextDisabled($"{module.Triggers.Count} triggers on this map");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Trigger shapes come from AreaTrigger.dbc (client data)\nand cannot be added or moved - only their database\neffects (teleport, tavern, quest, script) are editable."u8);

        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##filter"u8, "filter by id or name"u8, ref filter, 100);

        if (ImGui.BeginChild("##triggers"u8))
        {
            int shown = 0;
            foreach (var trigger in module.Triggers)
            {
                string label = module.DescribeTrigger(trigger.Id);
                if (filter.Length > 0 && !label.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
                shown++;

                if (ImGui.Selectable(label))
                    module.SelectedKey = trigger.Id;
                if (ImGui.IsItemHovered())
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        module.FlyTo((uint)module.CurrentMapId, trigger.Position);
                    ImGui.SetTooltip("Click: edit · double-click: fly camera to it"u8);
                }
            }
            if (module.Triggers.Count == 0)
                ImGui.TextDisabled("No area triggers on this map."u8);
            else if (shown == 0)
                ImGui.TextDisabled($"No triggers match \"{filter}\"");
        }
        ImGui.EndChild();
    }

    private void DrawEditor(uint key)
    {
        if (EditorWidgets.BackRow("All triggers", "Back to the trigger list (a shape click reopens the editor)"))
        {
            module.SelectedKey = null;
            return;
        }

        ImGui.TextUnformatted(module.DescribeTrigger(key));
        if (module.TryGetTrigger(key, out var shape))
        {
            ImGui.TextDisabled(shape.IsBox
                ? $"Box {shape.BoxHalf.X * 2:0.#} × {shape.BoxHalf.Y * 2:0.#} × {shape.BoxHalf.Z * 2:0.#}"
                : $"Sphere, radius {shape.Radius:0.#}");
            if (EditorWidgets.FlyToButton("trigger"))
                module.FlyTo((uint)module.CurrentMapId, shape.Position);
        }
        else
        {
            EditorWidgets.WrappedHint("The trigger is on another map (its destination is here)."u8);
        }

        DrawTeleport(key);
        DrawTavern(key);
        DrawQuest(key);
        DrawScript(key);
    }

    private void DrawTeleport(uint key)
    {
        ImGui.SeparatorText("Teleport"u8);
        if (!service.Teleports.TryGetValue(key, out var row))
        {
            ImGui.TextDisabled("Entering this trigger teleports nobody."u8);
            if (module.PlacementArmed)
            {
                if (ImGui.Button($"{Lucide.X} Cancel placement"))
                    module.PlacementArmed = false;
            }
            else
            {
                if (ImGui.Button("Place destination (click world)"u8))
                    module.PlacementArmed = true;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Then click the world where entering the trigger should teleport to"u8);
                ImGui.SameLine();
                if (ImGui.Button("Create at camera"u8))
                    module.CreateTeleportAtCamera(key);
            }
            return;
        }

        bool changed = false;

        string name = row.Name ?? "";
        EditorWidgets.FitNextItem("Name"u8);
        if (ImGui.InputText("Name"u8, ref name, 127))
        {
            row.Name = name;
            changed = true;
        }

        var dbc = gameContext.DbcManager;
        ImGui.TextDisabled($"Destination: {WorldPointNames.MapName(dbc, (int)row.Map)}");

        int targetMap = (int)row.Map;
        ImGui.SetNextItemWidth(90);
        if (ImGui.InputInt("Target map"u8, ref targetMap, 0, 0))
        {
            row.Map = (uint)Math.Max(0, targetMap);
            changed = true;
        }

        var pos = new System.Numerics.Vector3(row.Position.X, row.Position.Y, row.Position.Z);
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

        if (row.Map == (uint)module.CurrentMapId)
        {
            if (ImGui.SmallButton($"{Lucide.ArrowDownToLine} Snap to ground"))
            {
                row.Position = module.SnapToGround(row.Position);
                changed = true;
            }
            ImGui.SameLine();
        }
        if (EditorWidgets.FlyToButton("destination"))
            module.FlyTo(row.Map, row.Position);
        ImGui.SameLine();
        if (module.PlacementArmed)
        {
            if (ImGui.SmallButton($"{Lucide.X} Cancel placement"))
                module.PlacementArmed = false;
        }
        else
        {
            if (ImGui.SmallButton("Move destination (click world)"u8))
                module.PlacementArmed = true;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Click the world to move the destination there (always on the current map)"u8);
        }

        changed |= DrawRequirements(row);

        ImGui.Separator();
        DrawDeleteTeleport(row);

        if (changed)
            service.NotifyTeleportChanged(key);
    }

    private bool DrawRequirements(AreaTriggerTeleportData row)
    {
        var columns = service.TeleportColumns;
        bool changed = false;

        if ((columns & AreaTriggerTeleportColumns.Requirements) != 0)
        {
            ImGui.SeparatorText("Requirements"u8);

            int level = (int)row.RequiredLevel;
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt("Level"u8, ref level, 0, 0))
            {
                row.RequiredLevel = (uint)Math.Max(0, level);
                changed = true;
            }

            changed |= DrawItemField("##req_item", "Item", row.RequiredItem, v => row.RequiredItem = v, row);
            changed |= DrawItemField("##req_item2", "Item 2", row.RequiredItem2, v => row.RequiredItem2 = v, row);
            changed |= DrawQuestField("##req_quest", "Quest done", row.RequiredQuestDone, v => row.RequiredQuestDone = v, row);
        }

        if ((columns & AreaTriggerTeleportColumns.HeroicRequirements) != 0)
        {
            changed |= DrawItemField("##heroic_key", "Heroic key", row.HeroicKey, v => row.HeroicKey = v, row);
            changed |= DrawItemField("##heroic_key2", "Heroic key 2", row.HeroicKey2, v => row.HeroicKey2 = v, row);
            changed |= DrawQuestField("##heroic_quest", "Heroic quest", row.RequiredQuestDoneHeroic, v => row.RequiredQuestDoneHeroic = v, row);
        }

        if ((columns & AreaTriggerTeleportColumns.ConditionId) != 0)
        {
            int condition = (int)row.ConditionId;
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt("Condition id"u8, ref condition, 0, 0))
            {
                row.ConditionId = (uint)Math.Max(0, condition);
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("mangos_conditions entry checked on the entering player (0 = none)"u8);
        }

        if ((columns & AreaTriggerTeleportColumns.Status) != 0)
        {
            int status = (int)row.Status;
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt("Status"u8, ref status, 0, 0))
            {
                row.Status = (uint)Math.Max(0, status);
                changed = true;
            }
        }

        if ((columns & AreaTriggerTeleportColumns.StatusFailedText) != 0)
        {
            string failedText = row.StatusFailedText ?? "";
            EditorWidgets.FitNextItem("Failed text"u8);
            if (ImGui.InputText("Failed text"u8, ref failedText, 255))
            {
                row.StatusFailedText = failedText;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Message shown to the player when the requirements are not met"u8);
        }

        return changed;
    }

    private bool DrawItemField(string id, string label, uint value, Action<uint> setter, AreaTriggerTeleportData row)
    {
        bool changed = false;
        int v = (int)value;
        ImGui.SetNextItemWidth(90);
        if (ImGui.InputInt(label, ref v, 0, 0))
        {
            setter((uint)Math.Max(0, v));
            changed = true;
        }
        entryPicker.PickButton(id, "ItemParameter", "Pick an item from the list", (int)value, picked =>
        {
            setter((uint)Math.Max(0, picked));
            service.NotifyTeleportChanged(row.Id);
        });
        return changed;
    }

    private bool DrawQuestField(string id, string label, uint value, Action<uint> setter, AreaTriggerTeleportData row)
    {
        bool changed = false;
        int v = (int)value;
        ImGui.SetNextItemWidth(90);
        if (ImGui.InputInt(label, ref v, 0, 0))
        {
            setter((uint)Math.Max(0, v));
            changed = true;
        }
        entryPicker.PickButton(id, "QuestParameter", "Pick a quest from the list", (int)value, picked =>
        {
            setter((uint)Math.Max(0, picked));
            service.NotifyTeleportChanged(row.Id);
        });
        if (value > 0 && service.GetQuestName(value) is { } questName)
            ImGui.TextDisabled(questName);
        return changed;
    }

    private void DrawDeleteTeleport(AreaTriggerTeleportData row)
    {
        if (ImGui.Button($"{Lucide.Trash2} Delete teleport...", new System.Numerics.Vector2(-1, 0)))
            ImGui.OpenPopup("Delete areatrigger teleport"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Deletes the areatrigger_teleport row. Applied on Save.\nThe trigger stops teleporting players!"u8);

        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Delete areatrigger teleport", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextUnformatted($"Delete the teleport of areatrigger {row.Id}?");
        ImGui.TextDisabled("The database row is removed when you Save."u8);
        ImGui.Separator();
        if (ImGui.Button("Delete"u8, new System.Numerics.Vector2(120, 0)))
        {
            service.DeleteTeleport(row.Id);
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"u8, new System.Numerics.Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawTavern(uint key)
    {
        if (!service.SupportsTavern)
            return;

        ImGui.SeparatorText("Tavern"u8);
        bool isTavern = service.Taverns.TryGetValue(key, out var tavernName);
        if (ImGui.Checkbox("Rest area (tavern)"u8, ref isTavern))
            service.SetTavern(key, isTavern, isTavern ? tavernName ?? "" : null);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Players inside the trigger rest as if in an inn (areatrigger_tavern)"u8);

        if (isTavern && service.Taverns.ContainsKey(key))
        {
            string name = tavernName ?? "";
            EditorWidgets.FitNextItem("Tavern name"u8);
            if (ImGui.InputText("Tavern name"u8, ref name, 127))
                service.SetTavern(key, true, name);
        }
    }

    private void DrawQuest(uint key)
    {
        if (!service.SupportsQuestRelation)
            return;

        ImGui.SeparatorText("Exploration quest"u8);
        if (service.QuestRelations.TryGetValue(key, out var quest))
        {
            int q = (int)quest;
            ImGui.SetNextItemWidth(90);
            if (ImGui.InputInt("Quest"u8, ref q, 0, 0))
                service.SetQuestRelation(key, (uint)Math.Max(0, q));
            entryPicker.PickButton("##at_quest", "QuestParameter", "Pick a quest from the list", (int)quest,
                picked => service.SetQuestRelation(key, (uint)Math.Max(0, picked)));
            if (service.GetQuestName(quest) is { } questName)
                EditorWidgets.WrappedHint(questName);
            if (ImGui.SmallButton("Unlink quest"u8))
                service.SetQuestRelation(key, null);
        }
        else
        {
            ImGui.TextDisabled("Entering the trigger completes no quest."u8);
            entryPicker.PickButton("##at_quest_add", "QuestParameter",
                "Link a quest: entering the trigger completes its exploration objective\n(areatrigger_involvedrelation)", 0,
                picked =>
                {
                    if (picked > 0)
                        service.SetQuestRelation(key, (uint)picked);
                });
        }
    }

    private void DrawScript(uint key)
    {
        if (!service.SupportsScript)
            return;

        ImGui.SeparatorText("Script"u8);
        string script = service.ScriptNames.TryGetValue(key, out var s) ? s : "";
        EditorWidgets.FitNextItem("ScriptName"u8);
        if (ImGui.InputText("ScriptName"u8, ref script, 127))
            service.SetScriptName(key, script);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("scripted_areatrigger: the name must match a script in the core's\nscript library. Empty removes the row."u8);
    }
}
