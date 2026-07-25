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
            if (module.SelectedGroupId != 0)
                return "Click a grouped spawn: select its group · click ungrouped spawns: pick members to add";
            return module.Pending.Count == 0
                ? "Click spawns in the world to select them · click a grouped spawn to edit its group"
                : "Click: add/remove from the selection · name it in the panel to create a group";
        }
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\nspawn_group tables.");
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
        uint selected = module.SelectedGroupId;
        string preview = selected != 0 && service.GroupNames.TryGetValue(selected, out var n)
            ? $"{selected} {n}"
            : "Select a group to edit...";
        FieldWidth(selected != 0 ? 26 : 0);
        if (ImGui.BeginCombo("##grouppick", preview))
        {
            foreach (var (id, name) in service.GroupNames.OrderBy(kv => kv.Key))
            {
                if (ImGui.Selectable($"{id} {name}", id == selected))
                    module.SelectedGroupId = id;
            }
            ImGui.EndCombo();
        }
        if (selected != 0)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("x##deselect"))
                module.SelectedGroupId = 0;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Stop editing this group");
        }
        ImGui.Separator();
    }

    // ------------------------------------------------------- pending -> new group ----------------

    private void DrawPendingBuilder()
    {
        var pending = module.Pending;
        ImGui.TextDisabled($"{pending.Count} selected — click objects in the world");

        for (int i = pending.Count - 1; i >= 0; --i)
        {
            var s = pending[i];
            ImGui.PushID(i);
            if (ImGui.SmallButton("x"))
                pending.RemoveAt(i);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Remove from the selection (the spawn itself is untouched)");
            ImGui.SameLine();
            ImGui.TextUnformatted(SpawnGroupEditorModule.DescribeSpawn(s));
            ImGui.PopID();
        }

        ImGui.SeparatorText("New group");
        ImGui.BeginDisabled(pending.Count == 0);
        FieldWidth(64);
        ImGui.InputTextWithHint("##name", "group name", ref nameBuffer, 128);
        ImGui.SameLine();
        bool nameEmpty = string.IsNullOrWhiteSpace(nameBuffer);
        ImGui.BeginDisabled(nameEmpty);
        if (ImGui.Button("Create"))
        {
            var id = service.CreateGroup(nameBuffer, module.CollectPending());
            pending.Clear();
            nameBuffer = "";
            module.SelectedGroupId = id; // jump straight into the full editor
        }
        ImGui.EndDisabled();
        if (nameEmpty && pending.Count > 0 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Type a group name first");
        ImGui.EndDisabled();

        ImGui.SeparatorText("Existing group");
        ImGui.BeginDisabled(pending.Count == 0 || service.GroupNames.Count == 0);
        string preview = addToGroupId != 0 && service.GroupNames.TryGetValue(addToGroupId, out var n)
            ? $"{addToGroupId} {n}" : "Add to existing group...";
        FieldWidth(64);
        if (ImGui.BeginCombo("##addgroup", preview))
        {
            foreach (var (id, name) in service.GroupNames.OrderBy(kv => kv.Key))
            {
                if (ImGui.Selectable($"{id} {name}", id == addToGroupId))
                    addToGroupId = id;
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button("Add") && addToGroupId != 0)
        {
            service.AddToGroup(addToGroupId, module.CollectPending());
            pending.Clear();
        }
        ImGui.EndDisabled();
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
        if (ImGui.Button("Delete group...", new Vector2(-1, 0)))
            ImGui.OpenPopup("Delete spawn group");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Deletes the group with its formation, random entries, links and squads.\nMember spawns stay in the world, just ungrouped. Applied on Save.");

        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Delete spawn group", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        string name = service.GroupNames.TryGetValue(groupId, out var n) ? n : "";
        ImGui.TextUnformatted($"Delete group {groupId} \"{name}\"?");
        ImGui.TextDisabled("Member spawns stay in the world, just ungrouped.\nThe database rows are removed when you Save.");
        ImGui.Separator();
        if (ImGui.Button("Delete", new Vector2(120, 0)))
        {
            service.DeleteGroup(groupId);
            module.SelectedGroupId = 0;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawBasicName(uint groupId)
    {
        // basic cores: the name is the only editable template property, and only for new groups -
        // existing template rows aren't rewritten there
        ImGui.TextDisabled(service.GroupNames.TryGetValue(groupId, out var n) ? n : "");
    }

    private void DrawProperties(SpawnGroupDetails d)
    {
        bool changed = false;

        string name = d.Name;
        if (ImGui.InputText("Name", ref name, 200))
        {
            d.Name = name;
            changed = true;
        }

        ImGui.TextDisabled(d.Type == SpawnGroupTemplateType.Creature ? "Creature group" : "GameObject group");

        if (service.GroupFlags.Count > 0 && ImGui.TreeNodeEx("Flags", ImGuiTreeNodeFlags.None))
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

        if (ImGui.TreeNodeEx("Spawning", ImGuiTreeNodeFlags.None))
        {
            int maxCount = d.MaxCount;
            if (ImGui.InputInt("Max alive", ref maxCount))
            {
                d.MaxCount = Math.Max(0, maxCount);
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Maximum active alive entities spawned in the world (0 = all)");

            int worldState = d.WorldState;
            if (ImGui.InputInt("WorldState", ref worldState))
            {
                d.WorldState = worldState;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Condition id enabling spawning (0 = always; exclusive with the expression)");

            int wsExpression = d.WorldStateExpression;
            if (ImGui.InputInt("WS expression", ref wsExpression))
            {
                d.WorldStateExpression = wsExpression;
                changed = true;
            }

            int stringId = (int)d.StringId;
            if (ImGui.InputInt("String id", ref stringId))
            {
                d.StringId = (uint)Math.Max(0, stringId);
                changed = true;
            }

            bool overrideRespawn = d.RespawnOverrideMin.HasValue || d.RespawnOverrideMax.HasValue;
            if (ImGui.Checkbox("Override respawn time", ref overrideRespawn))
            {
                d.RespawnOverrideMin = overrideRespawn ? d.RespawnOverrideMin ?? 0 : null;
                d.RespawnOverrideMax = overrideRespawn ? d.RespawnOverrideMax : null;
                changed = true;
            }
            if (overrideRespawn)
            {
                int min = (int)(d.RespawnOverrideMin ?? 0);
                if (ImGui.InputInt("Respawn min (s)", ref min))
                {
                    d.RespawnOverrideMin = (uint)Math.Max(0, min);
                    changed = true;
                }
                int max = (int)(d.RespawnOverrideMax ?? d.RespawnOverrideMin ?? 0);
                if (ImGui.InputInt("Respawn max (s)", ref max))
                {
                    d.RespawnOverrideMax = (uint)Math.Max(0, max);
                    changed = true;
                }
                if (d.RespawnOverrideMin.HasValue && d.RespawnOverrideMax.HasValue &&
                    d.RespawnOverrideMin > d.RespawnOverrideMax)
                    ImGui.TextColored(EditorTheme.Warning, "Respawn min exceeds max");
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
        if (ImGui.BeginTable("members", slots ? 5 : 2, tableFlags))
        {
            // explicit weight: a long creature name must not inflate this column's share and
            // squeeze the fixed slot/chance inputs (or push the table past the panel width)
            ImGui.TableSetupColumn("Member", ImGuiTableColumnFlags.WidthStretch, 1f);
            if (slots)
            {
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 24);   // make-leader
                ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 52);
                ImGui.TableSetupColumn("Chance", ImGuiTableColumnFlags.WidthFixed, 52);
            }
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22);

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
                    label = "★ " + label; // the formation leader
                if (spawn != null)
                {
                    // row -> 3D sync: click highlights the spawn in the world, double-click flies to it
                    if (ImGui.Selectable(label))
                        module.HighlightSpawn(spawn, flyTo: false);
                    if (ImGui.IsItemHovered())
                    {
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            module.HighlightSpawn(spawn, flyTo: true);
                        ImGui.SetTooltip("Click: highlight in the world · double-click: fly camera to it");
                    }
                }
                else
                    ImGui.TextUnformatted(label);

                if (slots)
                {
                    var slot = details!.SlotOf(m.Guid);

                    ImGui.TableNextColumn();
                    ImGui.BeginDisabled(isLeader);
                    if (ImGui.SmallButton("★"))
                        module.MakeLeader(groupId, m.Guid);
                    ImGui.EndDisabled();
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(isLeader ? "Already the leader" : "Make this member the formation leader (slot 0)");

                    ImGui.TableNextColumn();
                    int slotId = slot.SlotId;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt("##slot", ref slotId, 0, 0))
                    {
                        slot.SlotId = Math.Max(-1, slotId);
                        details.MemberSlots[m.Guid] = slot;
                        changed = true;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Formation slot: 0 = leader, -1 = not part of the formation");

                    ImGui.TableNextColumn();
                    int chance = (int)slot.Chance;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.InputInt("##chance", ref chance, 0, 0))
                    {
                        if (chance is < 0 or > 100)
                            clampNote.Set($"Member chance is 0-100 - {chance} was clamped to {Math.Clamp(chance, 0, 100)}");
                        slot.Chance = (uint)Math.Clamp(chance, 0, 100);
                        details.MemberSlots[m.Guid] = slot;
                        changed = true;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Chance for this spawn to occur (0 = always)");
                }

                ImGui.TableNextColumn();
                if (ImGui.SmallButton("x"))
                    removeTarget = m;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove from the group (the spawn stays in the world; applied on Save)");

                ImGui.PopID();
            }
            ImGui.EndTable();

            if (removeTarget.HasValue)
                service.RemoveMember(groupId, removeTarget.Value);
        }

        if (slots && members.Count > 0)
        {
            if (ImGui.SmallButton("All in formation"))
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
                ImGui.SetTooltip("Assign formation slots to every member in the shown order");
            ImGui.SameLine();
            if (ImGui.SmallButton("Clear formation"))
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
                ImGui.SetTooltip("Take every member out of the formation (slot -1)");
        }

        if (module.Pending.Count > 0)
        {
            if (ImGui.Button($"Add {module.Pending.Count} selected spawn{(module.Pending.Count > 1 ? "s" : "")} to this group"))
            {
                service.AddToGroup(groupId, module.CollectPending());
                module.Pending.Clear();
            }
        }
        else
        {
            ImGui.TextDisabled("Click ungrouped spawns in the world to add them.");
        }

        if (changed)
            service.NotifyDetailsChanged(groupId);
    }

    private void DrawFormation(SpawnGroupDetails d)
    {
        ImGui.SeparatorText("Formation");

        bool has = d.Formation != null;
        bool changed = false;
        if (ImGui.Checkbox("Group moves in formation", ref has))
        {
            d.Formation = has ? new SpawnGroupFormationData() : null;
            changed = true;
        }

        if (d.Formation is { } f)
        {
            if (ImGui.BeginCombo("Shape", SpawnGroupFormationMath.ShapeName(f.Shape)))
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
            if (ImGui.SliderFloat("Spread", ref spread, -15f, 15f, "%.1f"))
            {
                f.Spread = spread;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Distance between formation members (core allows -15..15)");

            bool keepCompact = (f.Options & 0x02) != 0;
            if (ImGui.Checkbox("Keep compact", ref keepCompact))
            {
                f.Options = keepCompact ? f.Options | 0x02 : f.Options & ~0x02;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Dead members don't leave holes - the formation closes ranks");

            if (ImGui.BeginCombo("Movement", SpawnGroupFormationMath.MovementTypeName(f.MovementType)))
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
                if (ImGui.InputInt("Path id", ref pathId))
                {
                    f.PathId = Math.Max(0, pathId);
                    changed = true;
                }
                if (module.CanEditFormationPaths)
                {
                    ImGui.SameLine();
                    if (ImGui.SmallButton(f.PathId == 0 ? "Create path..." : "Edit path..."))
                        module.RequestOpenFormationPath(d);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Opens the path in the waypoint editor (waypoint_path)");
                }
            }

            string comment = f.Comment ?? "";
            if (ImGui.InputText("Comment", ref comment, 255))
            {
                f.Comment = comment;
                changed = true;
            }

            if (!d.MemberSlots.Values.Any(s => s.SlotId == 0))
                ImGui.TextColored(EditorTheme.Warning, "No leader: set a member's slot to 0");
            else
            {
                ImGui.TextDisabled("Translucent phantoms in the world preview the slots.");
                ImGui.BeginDisabled(!module.CanMoveMembersToSlots);
                if (ImGui.Button("Move spawns to formation slots", new Vector2(-1, 0)))
                    module.MoveMembersToSlots();
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Teleports every slotted member onto its previewed position,\nfacing the leader's heading (a spawn move - the toolbar Undo\nreverts it; persisted on Save)");
            }
        }

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    private void DrawRandomEntries(SpawnGroupDetails d)
    {
        ImGui.SeparatorText("Random entries");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Members spawned with entry 0 roll a random entry from this pool");

        bool changed = false;
        if (d.RandomEntries.Count > 0 &&
            ImGui.BeginTable("randentries", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Entry", ImGuiTableColumnFlags.WidthStretch, 1f);
            ImGui.TableSetupColumn("Min", ImGuiTableColumnFlags.WidthFixed, 44);
            ImGui.TableSetupColumn("Max", ImGuiTableColumnFlags.WidthFixed, 44);
            ImGui.TableSetupColumn("Chance", ImGuiTableColumnFlags.WidthFixed, 52);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22);
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
                if (ImGui.InputInt("##min", ref min, 0, 0))
                {
                    e.MinCount = (uint)Math.Max(0, min);
                    d.RandomEntries[i] = e;
                    changed = true;
                }

                ImGui.TableNextColumn();
                int max = (int)e.MaxCount;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt("##max", ref max, 0, 0))
                {
                    e.MaxCount = (uint)Math.Max(0, max);
                    d.RandomEntries[i] = e;
                    changed = true;
                }

                ImGui.TableNextColumn();
                int chance = (int)e.Chance;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt("##chance", ref chance, 0, 0))
                {
                    if (chance < 0)
                        clampNote.Set($"Chance can't be negative - {chance} was clamped to 0");
                    e.Chance = (uint)Math.Max(0, chance);
                    d.RandomEntries[i] = e;
                    changed = true;
                }

                ImGui.TableNextColumn();
                if (ImGui.SmallButton("x"))
                    removeAt = i;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove this random entry (applied on Save)");

                ImGui.PopID();
            }
            ImGui.EndTable();

            if (removeAt >= 0)
            {
                d.RandomEntries.RemoveAt(removeAt);
                changed = true;
            }

            if (d.RandomEntries.Any(e => e.MaxCount > 0 && e.MinCount > e.MaxCount))
                ImGui.TextColored(EditorTheme.Warning, "An entry has min > max");
        }

        ImGui.SetNextItemWidth(120);
        ImGui.InputInt("##newentry", ref newRandomEntry, 0, 0);
        entryPicker.PickButton("picknewentry", d.Type == SpawnGroupTemplateType.Creature, newRandomEntry,
            picked => newRandomEntry = (int)picked);
        ImGui.SameLine();
        if (ImGui.SmallButton("Add entry") && newRandomEntry > 0)
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
        ImGui.SeparatorText("Linked groups");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Linked groups spawn/despawn together with this one");

        bool changed = false;
        for (int i = 0; i < d.LinkedGroups.Count; ++i)
        {
            ImGui.PushID(i);
            if (ImGui.SmallButton("x"))
            {
                d.LinkedGroups.RemoveAt(i);
                changed = true;
                ImGui.PopID();
                break;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Unlink this group (applied on Save)");
            ImGui.SameLine();
            var id = d.LinkedGroups[i];
            ImGui.TextUnformatted(service.GroupNames.TryGetValue(id, out var n) ? $"{id} {n}" : id.ToString());
            ImGui.PopID();
        }

        string preview = linkGroupId != 0 && service.GroupNames.TryGetValue(linkGroupId, out var ln)
            ? $"{linkGroupId} {ln}" : "Link a group...";
        FieldWidth(52);
        if (ImGui.BeginCombo("##linkpick", preview))
        {
            foreach (var (id, name) in service.GroupNames.OrderBy(kv => kv.Key))
            {
                if (id == d.Id || d.LinkedGroups.Contains(id))
                    continue;
                if (ImGui.Selectable($"{id} {name}", id == linkGroupId))
                    linkGroupId = id;
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Link") && linkGroupId != 0 && linkGroupId != d.Id && !d.LinkedGroups.Contains(linkGroupId))
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
        ImGui.SeparatorText("Squads");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("A squad forces specific entries onto specific guids together\n(used with the random-entry pool)");

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
                    if (ImGui.SmallButton("x"))
                    {
                        d.Squads.RemoveAt(i);
                        changed = true;
                        ImGui.PopID();
                        break;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Remove from the squad (applied on Save)");
                    ImGui.SameLine();
                    var spawn = module.FindSpawn(new SpawnGroupMember(d.Type == SpawnGroupTemplateType.Creature, row.Guid));
                    ImGui.TextUnformatted(spawn != null ? SpawnGroupEditorModule.DescribeSpawn(spawn) : $"guid {row.Guid}");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90);
                    int entry = (int)row.Entry;
                    if (ImGui.InputInt("##entry", ref entry, 0, 0))
                    {
                        row.Entry = (uint)Math.Max(0, entry);
                        d.Squads[i] = row;
                        changed = true;
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Entry forced onto this guid when the squad is picked");
                    // the pick lands frames later - re-find the row by (squad, guid), the list
                    // may have changed (or the whole details object, after a reload) meanwhile
                    entryPicker.PickButton("pickentry", d.Type == SpawnGroupTemplateType.Creature, row.Entry,
                        MakeSquadEntrySetter(d.Id, row.SquadId, row.Guid));
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
        if (ImGui.SmallButton("Add squad"))
        {
            uint next = d.Squads.Count == 0 ? 1 : d.Squads.Max(s => s.SquadId) + 1;
            d.Squads.Add(new SpawnGroupSquadRow { SquadId = next, Guid = free.Guid, Entry = 0 });
            changed = true;
        }
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(free.Guid != 0
                ? "New squad, seeded with the first member not yet in a squad"
                : "Every group member is already in a squad - add more members first");

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

        if (ImGui.SmallButton("Add member..."))
            ImGui.OpenPopup("##addsquadmember");
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
