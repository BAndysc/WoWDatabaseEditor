using TheEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using WDE.Common.Database;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering.SpawnGroups;

/// <summary>
/// The Spawn-group tool's inspector section. Two modes:
///  - no group selected: the pending world-selection turns into a new group / joins an existing one;
///  - a group selected (click one of its members in the world, or pick it in the combo): the full
///    group editor - properties/flags, members with formation slots and chance, the formation
///    (7 CMaNGOS shapes + movement), the random-entry pool, linked groups and squads. Every
///    sub-section is capability-gated (<see cref="ISpawnGroupEditorService"/>), so basic cores
///    (Trinity) only see name + membership.
/// </summary>
public sealed class SpawnGroupInspector : IInspectorSection
{
    private readonly ISpawnGroupEditorService service;
    private readonly SpawnGroupEditorModule module;
    private readonly EntryPickerService entryPicker;

    private string nameBuffer = "";
    private uint addToGroupId;
    private int newRandomEntry;
    private uint linkGroupId;

    private readonly List<SpawnGroupMember> members = new();

    public SpawnGroupInspector(ISpawnGroupEditorService service, SpawnGroupEditorModule module,
        EntryPickerService entryPicker)
    {
        this.service = service;
        this.module = module;
        this.entryPicker = entryPicker;
    }

    public string Title => "Spawn groups";

    public bool IsDirty => service.IsSupported && service.AnyDirty;

    public Func<Task>? SaveSelf => IsDirty ? service.Save : null;

    public Func<Task>? RevertSelf => IsDirty ? (() => service.LoadForMap(service.LoadedMap)) : null;

    /// <summary>Right-aligned item width with a floor - a shrunken container must never collapse
    /// fields to a sliver.</summary>
    private static void FieldWidth(float reserve) =>
        ImGui.SetNextItemWidth(MathF.Max(90f, ImGui.GetContentRegionAvail().X - reserve));

    public string? Hints
    {
        get
        {
            if (!service.IsSupported)
                return "The current core has no spawn_group tables";
            if (service.MemberPickArmed)
                return module.SelectedGroupId != 0
                    ? "PICKING MEMBERS — click a spawn: add/remove it from the group · Esc/right-click: stop"
                    : "PICKING MEMBERS — click spawns: add/remove from the selection · Esc/right-click: stop";
            if (module.SelectedGroupId != 0)
                return "Click: select a spawn · \"Add/remove members\" in the panel edits membership · right-click: menu";
            return "Click a grouped spawn: edit its group · \"Pick members\" in the panel starts a new group";
        }
    }

    /// <summary>The armed pick-mode toggle both panel states share - green while armed (same
    /// language as the waypoint pen button).</summary>
    private void DrawPickToggle(string idleLabel, string armedLabel)
    {
        if (service.MemberPickArmed)
        {
            EditorTheme.PushArmedButton();
            if (ImGui.Button($"{Lucide.MousePointerClick} {armedLabel}  (Esc)", new Vector2(-1, 0)))
                service.MemberPickArmed = false;
            EditorTheme.PopButtonColors();
        }
        else
        {
            if (ImGui.Button($"{Lucide.MousePointerClick} {idleLabel}", new Vector2(-1, 0)))
                service.MemberPickArmed = true;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("While picking, world clicks add/remove instead of selecting.\nEsc or right-click stops picking."u8);
        }
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\nspawn_group tables."u8);
            return;
        }

        DrawGroupPicker();

        if (module.DecalCapNotice is { } capNotice)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorTheme.Warning);
            ImGui.TextWrapped(capNotice);
            ImGui.PopStyleColor();
        }

        if (module.SelectedGroupId != 0)
            DrawGroupEditor(module.SelectedGroupId);
        else
            DrawPendingBuilder();

        clampNote.Draw();

        DecalLegend.Draw(
            (SpawnGroupEditorModule.PendingDecalColor, "selection"),
            (SpawnGroupEditorModule.GroupDecalColor, "group member"),
            (SpawnGroupEditorModule.LeaderDecalColor, "leader"),
            (SpawnGroupEditorModule.GhostColor, "formation ghost slot"));
    }

    private readonly ClampNote clampNote = new();

    // ---------------------------------------------------------------- group picker ---------------

    private void DrawGroupPicker()
    {
        // no permanent combo: an open group shows a back row, otherwise one "Load existing"
        // button opens the filterable picker popup
        uint selected = module.SelectedGroupId;
        if (selected != 0)
        {
            if (EditorWidgets.BackRow("All groups", "Stop editing this group"))
                module.SelectedGroupId = 0;
            ImGui.SameLine();
            string name = service.GroupNames.TryGetValue(selected, out var n) ? n : "";
            ImGui.TextColored(EditorTheme.SelectionGold, $"{selected} {name}");
        }
        else if (EditorWidgets.LoadExistingPopup($"{Lucide.FolderOpen} Load existing...", "Load spawn group",
                     service.GroupNames.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)), out var picked))
            module.SelectedGroupId = picked;
        ImGui.Separator();
    }

    // ------------------------------------------------------- pending -> new group ----------------

    private void DrawPendingBuilder()
    {
        var pending = module.Pending;
        DrawPickToggle("Pick members (click spawns)", "Picking members — click spawns");
        ImGui.TextDisabled(pending.Count == 0 && !service.MemberPickArmed
            ? "No spawns picked yet."
            : $"{pending.Count} selected");

        for (int i = pending.Count - 1; i >= 0; --i)
        {
            var s = pending[i];
            ImGui.PushID(i);
            ImGui.TextUnformatted(SpawnGroupEditorModule.DescribeSpawn(s));
            if (EditorWidgets.TrailingRemoveButton("Remove from the selection (the spawn itself is untouched)"))
                pending.RemoveAt(i);
            ImGui.PopID();
        }

        ImGui.Spacing();
        if (EditorWidgets.CreateNamePopup($"{Lucide.Plus} New group...", "Create spawn group", "group name",
                ref nameBuffer, pending.Count > 0, "Select spawns in the world first", fullWidthButton: true) is { } name)
        {
            var id = service.CreateGroup(name, module.CollectPending());
            pending.Clear();
            module.SelectedGroupId = id; // jump straight into the full editor
        }

        // only offered once there is a selection to add - an always-visible disabled row is clutter
        if (pending.Count > 0 && service.GroupNames.Count > 0)
        {
            string preview = addToGroupId != 0 && service.GroupNames.TryGetValue(addToGroupId, out var n)
                ? $"{addToGroupId} {n}" : "Add to existing group...";
            FieldWidth(64);
            if (EditorWidgets.IdNameCombo("##addgroup", preview,
                    service.GroupNames.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value)),
                    addToGroupId, out var picked, service.GroupNames.Count))
                addToGroupId = picked;
            ImGui.SameLine();
            if (ImGui.Button($"{Lucide.Plus} Add") && addToGroupId != 0)
            {
                service.AddToGroup(addToGroupId, module.CollectPending());
                pending.Clear();
            }
        }
    }

    // --------------------------------------------------------------- group editor ----------------

    private void DrawGroupEditor(uint groupId)
    {
        // gone entirely (deleted/reloaded) - fall back to the builder like the pool editor does;
        // a null GetDetails with the group still listed is just a basic core (name-only view below)
        if (!service.GroupNames.ContainsKey(groupId))
        {
            module.SelectedGroupId = 0;
            clampNote.Set($"Spawn group {groupId} no longer exists - it was deleted or reloaded");
            return;
        }

        var details = service.GetDetails(groupId);

        if (details != null)
            DrawProperties(details);
        else
            DrawBasicName(groupId);

        DrawMembers(groupId, details);

        if (details != null)
        {
            if (service.SupportsFormations && details.Type == SpawnGroupTemplateType.Creature)
                DrawFormation(details);
            if (service.SupportsRandomEntries)
                DrawRandomEntries(details);
            if (service.SupportsLinkedGroups)
                DrawLinkedGroups(details);
            if (service.SupportsSquads)
                DrawSquads(details);
        }

        ImGui.Separator();
        DrawDeleteGroup(groupId);
    }

    private void DrawDeleteGroup(uint groupId)
    {
        if (ImGui.Button($"{Lucide.Trash2} Delete group...", new Vector2(-1, 0)))
            ImGui.OpenPopup("Delete spawn group"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Deletes the group with its formation, random entries, links and squads.\nMember spawns stay in the world, just ungrouped. Applied on Save."u8);

        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Delete spawn group", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        string name = service.GroupNames.TryGetValue(groupId, out var n) ? n : "";
        ImGui.TextUnformatted($"Delete group {groupId} \"{name}\"?");
        ImGui.TextDisabled("Member spawns stay in the world, just ungrouped.\nThe database rows are removed when you Save."u8);
        ImGui.Separator();
        if (ImGui.Button("Delete"u8, new Vector2(120, 0)))
        {
            service.DeleteGroup(groupId);
            module.SelectedGroupId = 0;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"u8, new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawBasicName(uint groupId)
    {
        // basic cores: the name is the only editable template property, and only for new groups -
        // existing template rows aren't rewritten there
        EditorWidgets.WrappedHint(service.GroupNames.TryGetValue(groupId, out var n) ? n : "");
    }

    private void DrawProperties(SpawnGroupDetails d)
    {
        bool changed = false;

        string name = d.Name;
        EditorWidgets.FitNextItem("Name"u8);
        if (ImGui.InputText("Name"u8, ref name, 200))
        {
            d.Name = name;
            changed = true;
        }

        ImGui.TextDisabled(d.Type == SpawnGroupTemplateType.Creature ? "Creature group"u8 : "GameObject group"u8);

        if (service.GroupFlags.Count > 0 && ImGui.TreeNodeEx("Flags"u8, ImGuiTreeNodeFlags.None))
        {
            foreach (var flag in service.GroupFlags)
            {
                bool creatureOnlyBlocked = flag.CreatureOnly && d.Type != SpawnGroupTemplateType.Creature;
                ImGui.BeginDisabled(creatureOnlyBlocked);
                bool set = (d.Flags & flag.Flag) != 0;
                if (ImGui.Checkbox(flag.Name, ref set))
                {
                    d.Flags = set ? d.Flags | flag.Flag : d.Flags & ~flag.Flag;
                    changed = true;
                }
                ImGui.EndDisabled();
                if (flag.Tooltip != null && ImGui.IsItemHovered())
                    ImGui.SetTooltip(flag.Tooltip);
            }
            ImGui.TreePop();
        }

        if (ImGui.TreeNodeEx("Spawning"u8, ImGuiTreeNodeFlags.None))
        {
            int maxCount = d.MaxCount;
            EditorWidgets.FitNextItem("Max alive"u8);
            if (ImGui.InputInt("Max alive"u8, ref maxCount))
            {
                d.MaxCount = Math.Max(0, maxCount);
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Maximum active alive entities spawned in the world (0 = all)"u8);

            int worldState = d.WorldState;
            EditorWidgets.FitNextItem("WorldState"u8);
            if (ImGui.InputInt("WorldState"u8, ref worldState))
            {
                d.WorldState = worldState;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Condition id enabling spawning (0 = always; exclusive with the expression)"u8);

            int wsExpression = d.WorldStateExpression;
            EditorWidgets.FitNextItem("WS expression"u8);
            if (ImGui.InputInt("WS expression"u8, ref wsExpression))
            {
                d.WorldStateExpression = wsExpression;
                changed = true;
            }

            int stringId = (int)d.StringId;
            EditorWidgets.FitNextItem("String id"u8);
            if (ImGui.InputInt("String id"u8, ref stringId))
            {
                d.StringId = (uint)Math.Max(0, stringId);
                changed = true;
            }

            bool overrideRespawn = d.RespawnOverrideMin.HasValue || d.RespawnOverrideMax.HasValue;
            if (ImGui.Checkbox("Override respawn time"u8, ref overrideRespawn))
            {
                d.RespawnOverrideMin = overrideRespawn ? d.RespawnOverrideMin ?? 0 : null;
                d.RespawnOverrideMax = overrideRespawn ? d.RespawnOverrideMax : null;
                changed = true;
            }
            if (overrideRespawn)
            {
                int min = (int)(d.RespawnOverrideMin ?? 0);
                EditorWidgets.FitNextItem("Respawn min (s)"u8);
                if (ImGui.InputInt("Respawn min (s)"u8, ref min))
                {
                    d.RespawnOverrideMin = (uint)Math.Max(0, min);
                    changed = true;
                }
                int max = (int)(d.RespawnOverrideMax ?? d.RespawnOverrideMin ?? 0);
                EditorWidgets.FitNextItem("Respawn max (s)"u8);
                if (ImGui.InputInt("Respawn max (s)"u8, ref max))
                {
                    d.RespawnOverrideMax = (uint)Math.Max(0, max);
                    changed = true;
                }
                if (d.RespawnOverrideMin.HasValue && d.RespawnOverrideMax.HasValue &&
                    d.RespawnOverrideMin > d.RespawnOverrideMax)
                    ImGui.TextColored(EditorTheme.Warning, "Respawn min exceeds max"u8);
            }
            ImGui.TreePop();
        }

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    private void DrawMembers(uint groupId, SpawnGroupDetails? details)
    {
        service.CollectMembers(groupId, members);
        // stable, formation-friendly order: leader first, then slots, unslotted by guid
        members.Sort((a, b) =>
        {
            int sa = details?.SlotOf(a.Guid).SlotId ?? -1;
            int sb = details?.SlotOf(b.Guid).SlotId ?? -1;
            if (sa != sb)
                return (sa < 0 ? int.MaxValue : sa).CompareTo(sb < 0 ? int.MaxValue : sb);
            return a.Guid.CompareTo(b.Guid);
        });

        ImGui.SeparatorText($"Members ({members.Count})");

        bool slots = details != null && service.SupportsFormationSlots() &&
                     details.Type == SpawnGroupTemplateType.Creature;
        bool changed = false;

        var tableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
        if (ImGui.BeginTable("members"u8, slots ? 5 : 2, tableFlags))
        {
            // explicit weight: a long creature name must not inflate this column's share and
            // squeeze the fixed slot/chance inputs (or push the table past the panel width)
            ImGui.TableSetupColumn("Member"u8, ImGuiTableColumnFlags.WidthStretch, 1f);
            if (slots)
            {
                ImGui.TableSetupColumn(""u8, ImGuiTableColumnFlags.WidthFixed, 28);   // make-leader
                ImGui.TableSetupColumn("Slot"u8, ImGuiTableColumnFlags.WidthFixed, 52);
                ImGui.TableSetupColumn("Chance"u8, ImGuiTableColumnFlags.WidthFixed, 52);
            }
            ImGui.TableSetupColumn(""u8, ImGuiTableColumnFlags.WidthFixed, 28);

            SpawnGroupMember? removeTarget = null;
            for (int i = 0; i < members.Count; ++i)
            {
                var m = members[i];
                ImGui.TableNextRow();
                ImGui.PushID((int)m.Guid);

                ImGui.TableNextColumn();
                var spawn = module.FindSpawn(m);
                string label = spawn != null ? SpawnGroupEditorModule.DescribeSpawn(spawn) : $"guid {m.Guid}";
                bool isLeader = slots && details!.SlotOf(m.Guid).SlotId == 0;
                if (isLeader)
                    label = Lucide.Crown + " " + label; // the formation leader
                if (spawn != null)
                {
                    // row -> 3D sync: click highlights the spawn in the world, double-click flies to it
                    if (ImGui.Selectable(label))
                        module.HighlightSpawn(spawn, flyTo: false);
                    if (ImGui.IsItemHovered())
                    {
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            module.HighlightSpawn(spawn, flyTo: true);
                        ImGui.SetTooltip("Click: highlight in the world · double-click: fly camera to it"u8);
                    }
                }
                else
                    ImGui.TextUnformatted(label);

                if (slots)
                {
                    var slot = details!.SlotOf(m.Guid);

                    ImGui.TableNextColumn();
                    ImGui.BeginDisabled(isLeader);
                    if (ImGui.SmallButton(Lucide.Crown))
                        module.MakeLeader(groupId, m.Guid);
                    ImGui.EndDisabled();
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(isLeader ? "Already the leader"u8 : "Make this member the formation leader (slot 0)"u8);

                    ImGui.TableNextColumn();
                    int slotId = slot.SlotId;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt("##slot"u8, ref slotId, 0, 0))
                    {
                        slot.SlotId = Math.Max(-1, slotId);
                        details.MemberSlots[m.Guid] = slot;
                        changed = true;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Formation slot: 0 = leader, -1 = not part of the formation"u8);

                    ImGui.TableNextColumn();
                    int chance = (int)slot.Chance;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt("##chance"u8, ref chance, 0, 0))
                    {
                        if (chance is < 0 or > 100)
                            clampNote.Set($"Member chance is 0-100 - {chance} was clamped to {Math.Clamp(chance, 0, 100)}");
                        slot.Chance = (uint)Math.Clamp(chance, 0, 100);
                        details.MemberSlots[m.Guid] = slot;
                        changed = true;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Chance for this spawn to occur (0 = always)"u8);
                }

                ImGui.TableNextColumn();
                if (ImGui.SmallButton(Lucide.X))
                    removeTarget = m;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove from the group (the spawn stays in the world; applied on Save)"u8);

                ImGui.PopID();
            }
            ImGui.EndTable();

            if (removeTarget.HasValue)
                service.RemoveMember(groupId, removeTarget.Value);
        }

        if (slots && members.Count > 0)
        {
            if (ImGui.SmallButton($"{Lucide.Users} All in formation"))
            {
                // keep the current leader if there is one, otherwise the first member leads;
                // everyone else gets 1..N in the shown (slot-sorted) order
                uint leaderGuid = details!.MemberSlots.FirstOrDefault(kv => kv.Value.SlotId == 0).Key;
                if (leaderGuid == 0 || members.All(m => m.Guid != leaderGuid))
                    leaderGuid = members[0].Guid;
                int next = 1;
                foreach (var m in members)
                {
                    var slot = details.SlotOf(m.Guid);
                    slot.SlotId = m.Guid == leaderGuid ? 0 : next++;
                    details.MemberSlots[m.Guid] = slot;
                }
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Assign formation slots to every member in the shown order"u8);
            ImGui.SameLine();
            if (ImGui.SmallButton($"{Lucide.X} Clear formation"))
            {
                foreach (var m in members)
                {
                    var slot = details!.SlotOf(m.Guid);
                    slot.SlotId = -1;
                    details.MemberSlots[m.Guid] = slot;
                }
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Take every member out of the formation (slot -1)"u8);
        }

        DrawPickToggle("Add/remove members (click spawns)", "Adding/removing members — click spawns");

        if (module.Pending.Count > 0 &&
            ImGui.Button($"Add {module.Pending.Count} selected spawn{(module.Pending.Count > 1 ? "s" : "")} to this group"))
        {
            service.AddToGroup(groupId, module.CollectPending());
            module.Pending.Clear();
        }

        DrawAddByGuid(groupId);

        if (changed)
            service.NotifyDetailsChanged(groupId);
    }

    private int addMemberGuid;
    private string? addMemberError;
    private uint addMemberErrorGroup;

    // typing a guid covers members the click gesture can't reach (unloaded areas, occluded,
    // inside buildings) - same affordance the formation editor already has
    private void DrawAddByGuid(uint groupId)
    {
        ImGui.SetNextItemWidth(90);
        ImGui.InputInt("##addbyguid"u8, ref addMemberGuid, 0, 0);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Guid of a spawn on this map"u8);
        ImGui.SameLine();
        ImGui.BeginDisabled(addMemberGuid <= 0);
        if (ImGui.SmallButton($"{Lucide.Plus} Add by guid"))
        {
            addMemberError = module.TryAddMemberByGuid(groupId, (uint)addMemberGuid);
            addMemberErrorGroup = groupId;
            if (addMemberError == null)
                addMemberGuid = 0;
        }
        ImGui.EndDisabled();
        if (addMemberError != null && addMemberErrorGroup == groupId)
            ImGui.TextColored(EditorTheme.Warning, addMemberError);
    }

    private void DrawFormation(SpawnGroupDetails d)
    {
        // "Group formation" - the standalone tool for creature_formations is "Creature formations";
        // the two systems must not share one name
        ImGui.SeparatorText("Group formation"u8);

        bool has = d.Formation != null;
        bool changed = false;
        if (ImGui.Checkbox("Group moves in formation"u8, ref has))
        {
            d.Formation = has ? new SpawnGroupFormationData() : null;
            changed = true;
        }

        if (d.Formation is { } f)
        {
            EditorWidgets.FitNextItem("Shape"u8);
            if (ImGui.BeginCombo("Shape"u8, SpawnGroupFormationMath.ShapeName(f.Shape)))
            {
                for (int i = 0; i <= 6; ++i)
                {
                    var shape = (FormationShape)i;
                    if (ImGui.Selectable(SpawnGroupFormationMath.ShapeName(shape), shape == f.Shape))
                    {
                        f.Shape = shape;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            float spread = f.Spread;
            EditorWidgets.FitNextItem("Spread"u8);
            if (ImGui.SliderFloat("Spread"u8, ref spread, -15f, 15f, "%.1f"u8))
            {
                f.Spread = spread;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Distance between formation members (core allows -15..15)"u8);

            bool keepCompact = (f.Options & 0x02) != 0;
            if (ImGui.Checkbox("Keep compact"u8, ref keepCompact))
            {
                f.Options = keepCompact ? f.Options | 0x02 : f.Options & ~0x02;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Dead members don't leave holes - the formation closes ranks"u8);

            EditorWidgets.FitNextItem("Movement"u8);
            if (ImGui.BeginCombo("Movement"u8, SpawnGroupFormationMath.MovementTypeName(f.MovementType)))
            {
                for (int i = 0; i <= 4; ++i)
                {
                    var type = (MovementType)i;
                    if (ImGui.Selectable(SpawnGroupFormationMath.MovementTypeName(type), type == f.MovementType))
                    {
                        f.MovementType = type;
                        changed = true;
                    }
                }
                ImGui.EndCombo();
            }

            if (f.MovementType is MovementType.Waypoint or MovementType.SplinePath or MovementType.LinearPath)
            {
                int pathId = f.PathId;
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputInt("Path id"u8, ref pathId))
                {
                    f.PathId = Math.Max(0, pathId);
                    changed = true;
                }
                if (module.CanEditFormationPaths)
                {
                    ImGui.SameLine();
                    if (ImGui.SmallButton(f.PathId == 0 ? $"{Lucide.Route} Create path..." : $"{Lucide.Route} Edit path..."))
                        module.RequestOpenFormationPath(d);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Opens the path in the waypoint editor (waypoint_path)"u8);
                }
            }

            string comment = f.Comment ?? "";
            EditorWidgets.FitNextItem("Comment"u8);
            if (ImGui.InputText("Comment"u8, ref comment, 255))
            {
                f.Comment = comment;
                changed = true;
            }

            if (!d.MemberSlots.Values.Any(s => s.SlotId == 0))
                ImGui.TextColored(EditorTheme.Warning, "No leader: set a member's slot to 0"u8);
            else
            {
                EditorWidgets.WrappedHint("Translucent phantoms in the world preview the slots."u8);
                ImGui.BeginDisabled(!module.CanMoveMembersToSlots);
                if (ImGui.Button("Move spawns to formation slots"u8, new Vector2(-1, 0)))
                    module.MoveMembersToSlots();
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Teleports every slotted member onto its previewed position,\nfacing the leader's heading (a spawn move - the toolbar Undo\nreverts it; persisted on Save)"u8);
            }
        }

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    private void DrawRandomEntries(SpawnGroupDetails d)
    {
        ImGui.SeparatorText("Random entries"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Members spawned with entry 0 roll a random entry from this pool"u8);

        bool changed = false;
        if (d.RandomEntries.Count > 0 &&
            ImGui.BeginTable("randentries"u8, 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Entry"u8, ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableSetupColumn("Min"u8, ImGuiTableColumnFlags.WidthFixed, 44);
            ImGui.TableSetupColumn("Max"u8, ImGuiTableColumnFlags.WidthFixed, 44);
            ImGui.TableSetupColumn("Chance"u8, ImGuiTableColumnFlags.WidthFixed, 52);
            ImGui.TableSetupColumn(""u8, ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.TableHeadersRow();

            int removeAt = -1;
            for (int i = 0; i < d.RandomEntries.Count; ++i)
            {
                var e = d.RandomEntries[i];
                ImGui.TableNextRow();
                ImGui.PushID(i);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(e.Entry.ToString());

                ImGui.TableNextColumn();
                int min = (int)e.MinCount;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt("##min"u8, ref min, 0, 0))
                {
                    e.MinCount = (uint)Math.Max(0, min);
                    d.RandomEntries[i] = e;
                    changed = true;
                }

                ImGui.TableNextColumn();
                int max = (int)e.MaxCount;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt("##max"u8, ref max, 0, 0))
                {
                    e.MaxCount = (uint)Math.Max(0, max);
                    d.RandomEntries[i] = e;
                    changed = true;
                }

                ImGui.TableNextColumn();
                int chance = (int)e.Chance;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt("##chance"u8, ref chance, 0, 0))
                {
                    if (chance < 0)
                        clampNote.Set($"Chance can't be negative - {chance} was clamped to 0");
                    e.Chance = (uint)Math.Max(0, chance);
                    d.RandomEntries[i] = e;
                    changed = true;
                }

                ImGui.TableNextColumn();
                if (ImGui.SmallButton(Lucide.X))
                    removeAt = i;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove this random entry (applied on Save)"u8);

                ImGui.PopID();
            }
            ImGui.EndTable();

            if (removeAt >= 0)
            {
                d.RandomEntries.RemoveAt(removeAt);
                changed = true;
            }

            if (d.RandomEntries.Any(e => e.MaxCount > 0 && e.MinCount > e.MaxCount))
                ImGui.TextColored(EditorTheme.Warning, "An entry has min > max"u8);
        }

        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("##newentry"u8, ref newRandomEntry, 0, 0);
        entryPicker.PickButton("picknewentry", d.Type == SpawnGroupTemplateType.Creature, newRandomEntry,
            picked => newRandomEntry = (int)picked);
        ImGui.SameLine();
        if (ImGui.SmallButton($"{Lucide.Plus} Add entry") && newRandomEntry > 0)
        {
            d.RandomEntries.Add(new SpawnGroupRandomEntryRow { Entry = (uint)newRandomEntry, Chance = 0 });
            newRandomEntry = 0;
            changed = true;
        }

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    private void DrawLinkedGroups(SpawnGroupDetails d)
    {
        ImGui.SeparatorText("Linked groups"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Linked groups spawn/despawn together with this one"u8);

        bool changed = false;
        for (int i = 0; i < d.LinkedGroups.Count; ++i)
        {
            ImGui.PushID(i);
            var id = d.LinkedGroups[i];
            ImGui.TextUnformatted(service.GroupNames.TryGetValue(id, out var n) ? $"{id} {n}" : id.ToString());
            if (EditorWidgets.TrailingRemoveButton("Unlink this group (applied on Save)"))
            {
                d.LinkedGroups.RemoveAt(i);
                changed = true;
                ImGui.PopID();
                break;
            }
            ImGui.PopID();
        }

        string preview = linkGroupId != 0 && service.GroupNames.TryGetValue(linkGroupId, out var ln)
            ? $"{linkGroupId} {ln}" : "Link a group...";
        FieldWidth(52);
        if (EditorWidgets.IdNameCombo("##linkpick", preview,
                service.GroupNames.OrderBy(kv => kv.Key)
                    .Where(kv => kv.Key != d.Id && !d.LinkedGroups.Contains(kv.Key))
                    .Select(kv => (kv.Key, kv.Value)),
                linkGroupId, out var picked, service.GroupNames.Count))
            linkGroupId = picked;
        ImGui.SameLine();
        if (ImGui.SmallButton($"{Lucide.Link} Link") && linkGroupId != 0 && linkGroupId != d.Id && !d.LinkedGroups.Contains(linkGroupId))
        {
            d.LinkedGroups.Add(linkGroupId);
            linkGroupId = 0;
            changed = true;
        }

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    private void DrawSquads(SpawnGroupDetails d)
    {
        ImGui.SeparatorText("Squads"u8);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("A squad forces specific entries onto specific guids together\n(used with the random-entry pool)"u8);

        bool changed = false;
        foreach (var squadId in d.Squads.Select(s => s.SquadId).Distinct().OrderBy(x => x).ToList())
        {
            ImGui.PushID((int)squadId);
            if (ImGui.TreeNodeEx($"Squad {squadId}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                for (int i = 0; i < d.Squads.Count; ++i)
                {
                    if (d.Squads[i].SquadId != squadId)
                        continue;
                    var row = d.Squads[i];
                    ImGui.PushID(i);
                    var spawn = module.FindSpawn(new SpawnGroupMember(d.Type == SpawnGroupTemplateType.Creature, row.Guid));
                    ImGui.TextUnformatted(spawn != null ? SpawnGroupEditorModule.DescribeSpawn(spawn) : $"guid {row.Guid}");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90);
                    int entry = (int)row.Entry;
                    if (ImGui.InputInt("##entry"u8, ref entry, 0, 0))
                    {
                        row.Entry = (uint)Math.Max(0, entry);
                        d.Squads[i] = row;
                        changed = true;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Entry forced onto this guid when the squad is picked"u8);
                    // the pick lands frames later - re-find the row by (squad, guid), the list
                    // may have changed (or the whole details object, after a reload) meanwhile
                    entryPicker.PickButton("pickentry", d.Type == SpawnGroupTemplateType.Creature, row.Entry,
                        MakeSquadEntrySetter(d.Id, row.SquadId, row.Guid));
                    if (EditorWidgets.TrailingRemoveButton("Remove from the squad (applied on Save)"))
                    {
                        d.Squads.RemoveAt(i);
                        changed = true;
                        ImGui.PopID();
                        break;
                    }
                    ImGui.PopID();
                }

                DrawAddSquadMember(d, squadId, ref changed);
                ImGui.TreePop();
            }
            ImGui.PopID();
        }

        // an empty squad has no rows, so a new one is seeded with the first unsquadded member -
        // when there is none the button disables instead of silently doing nothing
        service.CollectMembers(d.Id, members);
        var free = members.FirstOrDefault(m => d.Squads.All(s => s.Guid != m.Guid));
        ImGui.BeginDisabled(free.Guid == 0);
        if (ImGui.SmallButton($"{Lucide.Plus} Add squad"))
        {
            uint next = d.Squads.Count == 0 ? 1 : d.Squads.Max(s => s.SquadId) + 1;
            d.Squads.Add(new SpawnGroupSquadRow { SquadId = next, Guid = free.Guid, Entry = 0 });
            changed = true;
        }
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(free.Guid != 0
                ? "New squad, seeded with the first member not yet in a squad"u8
                : "Every group member is already in a squad - add more members first"u8);

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    private Action<uint> MakeSquadEntrySetter(uint groupId, uint squadId, uint guid) => picked =>
    {
        if (service.GetDetails(groupId) is not { } details)
            return;
        for (int i = 0; i < details.Squads.Count; ++i)
        {
            if (details.Squads[i].SquadId != squadId || details.Squads[i].Guid != guid)
                continue;
            var row = details.Squads[i];
            row.Entry = picked;
            details.Squads[i] = row;
            service.NotifyDetailsChanged(groupId);
            return;
        }
    };

    private void DrawAddSquadMember(SpawnGroupDetails d, uint squadId, ref bool changed)
    {
        service.CollectMembers(d.Id, members);
        var candidates = members.Where(m => d.Squads.All(s => s.Guid != m.Guid)).ToList();
        if (candidates.Count == 0)
            return;

        if (ImGui.SmallButton($"{Lucide.Plus} Add member..."))
            ImGui.OpenPopup("##addsquadmember"u8);
        if (ImGuiEx.BeginPopup("##addsquadmember"))
        {
            foreach (var m in candidates)
            {
                var spawn = module.FindSpawn(m);
                if (ImGui.Selectable(spawn != null ? SpawnGroupEditorModule.DescribeSpawn(spawn) : $"guid {m.Guid}"))
                {
                    d.Squads.Add(new SpawnGroupSquadRow { SquadId = squadId, Guid = m.Guid, Entry = 0 });
                    changed = true;
                }
            }
            ImGui.EndPopup();
        }
    }
}

internal static class SpawnGroupCapabilityExtensions
{
    /// <summary>Slots live in spawn_group_spawn, which every supported core has - but only cores
    /// with the formation system actually use them.</summary>
    public static bool SupportsFormationSlots(this ISpawnGroupEditorService service) =>
        service.SupportsFormations;
}
