using TheEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using WDE.Common.Database;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Pools;

namespace WDE.MapSpawns.Rendering.Pools;

/// <summary>
/// The Spawn-pool tool's inspector section. Two modes:
///  - no pool selected: the pending world-selection turns into a new pool / joins an existing one;
///  - a pool selected (click one of its members in the world, or pick it in the combo): the full
///    pool editor - description/max_limit, guid members with chance, entry-wide members, the mother
///    pool and child pools. Every sub-section is capability-gated (<see cref="IPoolEditorService"/>).
/// The CMaNGOS chance rules (explicit chance only honored at max_limit 1, chance 0 = equal roll)
/// surface as warnings - nothing here ever hard-blocks a save.
/// </summary>
public sealed class PoolInspector : IInspectorSection
{
    private readonly IPoolEditorService service;
    private readonly PoolEditorModule module;
    private readonly ICachedDatabaseProvider cachedDatabase;
    private readonly EntryPickerService entryPicker;

    private string descriptionBuffer = "";
    private uint addToPoolId;
    private int newEntryId;
    private bool newEntryIsCreature = true;

    private readonly List<PoolMember> members = new();
    private readonly List<uint> children = new();

    private static readonly Vector4 WarningColor = EditorTheme.Warning;

    public PoolInspector(IPoolEditorService service, PoolEditorModule module,
        ICachedDatabaseProvider cachedDatabase, EntryPickerService entryPicker)
    {
        this.service = service;
        this.module = module;
        this.cachedDatabase = cachedDatabase;
        this.entryPicker = entryPicker;
    }

    public string Title => "Spawn pools";

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
                return "The current core has no pool tables";
            if (module.SelectedPoolId != 0)
                return "Click a pooled spawn: select its pool · click unpooled spawns: pick members to add";
            return module.Pending.Count == 0
                ? "Click spawns in the world to select them · click a pooled spawn to edit its pool"
                : "Click: add/remove from the selection · describe it in the panel to create a pool";
        }
    }

    public void DrawContent()
    {
        if (!service.IsSupported)
        {
            ImGui.TextDisabled("The current core has no\npool tables.");
            return;
        }

        DrawPoolPicker();

        if (module.DecalCapNotice is { } capNotice)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
            ImGui.TextWrapped(capNotice);
            ImGui.PopStyleColor();
        }

        if (module.SelectedPoolId != 0)
            DrawPoolEditor(module.SelectedPoolId);
        else
            DrawPendingBuilder();

        clampNote.Draw();

        DecalLegend.Draw(
            (PoolEditorModule.PendingDecalColor, "selection"),
            (PoolEditorModule.MemberDecalColor, "pool member"),
            (PoolEditorModule.EntryPooledDecalColor, "entry-wide pooled"),
            (PoolEditorModule.ChildMemberDecalColor, "child pool member"));
    }

    private readonly ClampNote clampNote = new();

    // chance fields clamp to 0-100 - always tell the user their typed value was adjusted
    private float ClampChance(float chance, string field)
    {
        if (chance is < 0f or > 100f)
            clampNote.Set($"{field} is 0-100% - {chance:0.#} was clamped to {Math.Clamp(chance, 0f, 100f):0.#}");
        return Math.Clamp(chance, 0f, 100f);
    }

    // ----------------------------------------------------------------- pool picker ---------------

    private void DrawPoolPicker()
    {
        uint selected = module.SelectedPoolId;
        string preview = selected != 0 && service.PoolNames.TryGetValue(selected, out var n)
            ? $"{selected} {n}"
            : "Select a pool to edit...";
        FieldWidth(selected != 0 ? 26 : 0);
        if (ImGui.BeginCombo("##poolpick", preview))
        {
            foreach (var (id, name) in service.PoolNames.OrderBy(kv => kv.Key))
            {
                if (ImGui.Selectable($"{id} {name}", id == selected))
                    module.SelectedPoolId = id;
            }
            ImGui.EndCombo();
        }
        if (selected != 0)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("x##deselect"))
                module.SelectedPoolId = 0;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Stop editing this pool");
        }
        ImGui.Separator();
    }

    // --------------------------------------------------------- pending -> new pool ---------------

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
            ImGui.SameLine();
            ImGui.TextUnformatted(PoolEditorModule.DescribeSpawn(s));
            ImGui.PopID();
        }

        ImGui.SeparatorText("New pool");
        ImGui.BeginDisabled(pending.Count == 0);
        FieldWidth(64);
        ImGui.InputTextWithHint("##description", "pool description", ref descriptionBuffer, 128);
        ImGui.SameLine();
        bool descriptionEmpty = string.IsNullOrWhiteSpace(descriptionBuffer);
        ImGui.BeginDisabled(descriptionEmpty);
        if (ImGui.Button("Create"))
        {
            var id = service.CreatePool(descriptionBuffer, module.CollectPending());
            pending.Clear();
            descriptionBuffer = "";
            module.SelectedPoolId = id; // jump straight into the full editor
        }
        ImGui.EndDisabled();
        if (descriptionEmpty && pending.Count > 0 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Type a pool description first");
        ImGui.EndDisabled();

        ImGui.SeparatorText("Existing pool");
        ImGui.BeginDisabled(pending.Count == 0 || service.PoolNames.Count == 0);
        string preview = addToPoolId != 0 && service.PoolNames.TryGetValue(addToPoolId, out var n)
            ? $"{addToPoolId} {n}" : "Add to existing pool...";
        FieldWidth(64);
        if (ImGui.BeginCombo("##addpool", preview))
        {
            foreach (var (id, name) in service.PoolNames.OrderBy(kv => kv.Key))
            {
                if (ImGui.Selectable($"{id} {name}", id == addToPoolId))
                    addToPoolId = id;
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button("Add") && addToPoolId != 0)
        {
            service.AddToPool(addToPoolId, module.CollectPending());
            pending.Clear();
        }
        ImGui.EndDisabled();
    }

    // ---------------------------------------------------------------- pool editor ----------------

    private void DrawPoolEditor(uint poolId)
    {
        var details = service.GetDetails(poolId);
        if (details == null)
        {
            // deleted (or never loaded) - fall back to the builder flow, but say WHY the editor vanished
            module.SelectedPoolId = 0;
            clampNote.Set($"Pool {poolId} no longer exists - it was deleted or reloaded");
            return;
        }

        DrawProperties(details);
        if (service.SupportsNestedPools)
            DrawMotherPool(details);
        DrawMembers(poolId, details);
        if (service.SupportsEntryPooling)
            DrawEntryMembers(details);
        if (service.SupportsNestedPools)
            DrawChildPools(details);

        ImGui.Separator();
        DrawDeletePool(poolId);
    }

    private void DrawProperties(PoolDetails d)
    {
        bool changed = false;

        string description = d.Description;
        if (ImGui.InputText("Description", ref description, 255))
        {
            d.Description = description;
            changed = true;
        }

        int maxLimit = (int)d.MaxLimit;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputInt("Max limit", ref maxLimit))
        {
            d.MaxLimit = (uint)Math.Max(0, maxLimit);
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Max members spawned at once; 0 = no limit");

        // advisory only - the core just behaves surprisingly, saving is always allowed
        int spawnableUnits = CountSpawnableUnits(d);
        if (d.MaxLimit != 0 && spawnableUnits > 0 && d.MaxLimit >= spawnableUnits)
            ImGui.TextColored(WarningColor, $"Max limit ≥ {spawnableUnits} members:\nevery member always spawns");
        if (service.ExplicitChanceRequiresMaxLimitOne && d.MaxLimit != 1 && HasExplicitChances(d))
            ImGui.TextColored(WarningColor, "Explicit chances are only honored\nat max limit 1 - equal roll is used");

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    /// <summary>How many things the pool can roll between: direct members + entry-wide members +
    /// child pools.</summary>
    private int CountSpawnableUnits(PoolDetails d)
    {
        service.CollectMembers(d.Id, members);
        service.CollectChildren(d.Id, children);
        return members.Count + d.EntryMembers.Count + children.Count;
    }

    private bool HasExplicitChances(PoolDetails d)
    {
        service.CollectMembers(d.Id, members);
        foreach (var m in members)
        {
            if (d.DataOf(m).Chance > 0)
                return true;
        }
        foreach (var data in d.EntryMembers.Values)
        {
            if (data.Chance > 0)
                return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- mother pool ----------------

    private bool changingMother; // the re-parent combo only appears after an explicit "Change..."
    private uint changingMotherOfPool;

    private void DrawMotherPool(PoolDetails d)
    {
        if (changingMother && changingMotherOfPool != d.Id)
            changingMother = false; // the in-flight re-parent belongs to a previously shown pool
        changingMotherOfPool = d.Id;

        ImGui.SeparatorText("Mother pool");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("This pool becomes one rollable member of its mother pool (pool_pool)");

        // read-only display + explicit actions - the old always-open combo re-parented on a stray
        // click when the user only wanted to SEE the mother
        if (d.MotherPool is { } mother)
        {
            ImGui.TextUnformatted(service.PoolNames.TryGetValue(mother, out var mn) ? $"{mother} {mn}" : mother.ToString());
            ImGui.SameLine();
            if (ImGui.SmallButton("Open##gotomother"))
            {
                changingMother = false;
                module.SelectedPoolId = mother;
                return;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Edit the mother pool here (jump back via its child list)");
            ImGui.SameLine();
            if (ImGui.SmallButton(changingMother ? "Cancel##mother" : "Change...##mother"))
                changingMother = !changingMother;
            ImGui.SameLine();
            if (ImGui.SmallButton("x##unmother"))
                service.TrySetMotherPool(d.Id, null);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Detach from the mother pool (becomes top-level)");
        }
        else
        {
            ImGui.TextDisabled("Top-level pool");
            ImGui.SameLine();
            if (ImGui.SmallButton(changingMother ? "Cancel##mother" : "Set mother...##mother"))
                changingMother = !changingMother;
        }

        if (changingMother)
        {
            FieldWidth(0);
            if (ImGui.BeginCombo("##motherpick", "Pick the new mother pool..."))
            {
                foreach (var (id, name) in service.PoolNames.OrderBy(kv => kv.Key))
                {
                    if (!CanBeMotherOf(d.Id, id))
                        continue;
                    if (ImGui.Selectable($"{id} {name}", id == d.MotherPool))
                    {
                        service.TrySetMotherPool(d.Id, id);
                        changingMother = false;
                    }
                }
                ImGui.EndCombo();
            }
        }

        if (d.MotherPool != null)
        {
            float chance = d.MotherChance;
            ImGui.SetNextItemWidth(120);
            if (ImGui.InputFloat("Chance in mother", ref chance, 0, 0, "%.1f"))
            {
                d.MotherChance = ClampChance(chance, "Chance in mother");
                service.NotifyDetailsChanged(d.Id);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Roll chance of this pool within its mother (0 = equal-chance)");
        }
    }

    /// <summary>A candidate can mother this pool unless it IS the pool or descends from it (the
    /// same walk <see cref="IPoolEditorService.TrySetMotherPool"/> rejects - hidden here so the
    /// combo only offers valid picks).</summary>
    private bool CanBeMotherOf(uint poolId, uint candidate)
    {
        if (candidate == poolId)
            return false;
        for (uint? walk = candidate; walk != null; walk = service.MotherOf(walk.Value))
        {
            if (walk == poolId)
                return false;
        }
        return true;
    }

    // -------------------------------------------------------------------- members ----------------

    private void DrawMembers(uint poolId, PoolDetails d)
    {
        service.CollectMembers(poolId, members);
        members.Sort((a, b) => a.IsCreature != b.IsCreature
            ? (a.IsCreature ? -1 : 1)
            : a.Guid.CompareTo(b.Guid));

        ImGui.SeparatorText($"Members ({members.Count})");

        bool changed = false;
        float chanceSum = 0;
        var tableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
        // table id must be unique across ALL inspector sections - they share the "##inspector"
        // child (same ID stack), and ImGui persists table column widths by ID, so reusing the
        // spawn-group inspector's "members" id would inherit its 5-column layout (1px columns)
        if (members.Count > 0 && ImGui.BeginTable("poolmembers", 3, tableFlags))
        {
            ImGui.TableSetupColumn("Member");
            ImGui.TableSetupColumn("Chance", ImGuiTableColumnFlags.WidthFixed, 52);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22);

            PoolMember? removeTarget = null;
            foreach (var m in members)
            {
                ImGui.TableNextRow();
                ImGui.PushID(m.IsCreature ? (int)m.Guid : -(int)m.Guid);

                ImGui.TableNextColumn();
                var spawn = module.FindSpawn(m);
                var data = d.DataOf(m);
                string memberLabel = spawn != null ? PoolEditorModule.DescribeSpawn(spawn)
                    : $"{(m.IsCreature ? "creature" : "gameobject")} guid {m.Guid}";
                if (spawn != null)
                {
                    // row -> 3D sync: click highlights the spawn in the world, double-click flies to it
                    if (ImGui.Selectable(memberLabel))
                        module.HighlightSpawn(spawn, flyTo: false);
                    if (ImGui.IsItemHovered())
                    {
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            module.HighlightSpawn(spawn, flyTo: true);
                        ImGui.SetTooltip(data.Description is { Length: > 0 } desc
                            ? $"{desc}\nClick: highlight in the world · double-click: fly camera to it"
                            : "Click: highlight in the world · double-click: fly camera to it");
                    }
                }
                else
                {
                    ImGui.TextUnformatted(memberLabel);
                    if (data.Description is { Length: > 0 } memberDescription && ImGui.IsItemHovered())
                        ImGui.SetTooltip(memberDescription);
                }

                ImGui.TableNextColumn();
                float chance = data.Chance;
                chanceSum += chance;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputFloat("##chance", ref chance, 0, 0, "%.1f"))
                {
                    data.Chance = ClampChance(chance, "Member chance");
                    d.SetData(m, data);
                    changed = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Explicit roll chance in percent (0 = equal-chance member)");

                ImGui.TableNextColumn();
                if (ImGui.SmallButton("x"))
                    removeTarget = m;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove from the pool (the spawn stays in the world; applied on Save)");

                ImGui.PopID();
            }
            ImGui.EndTable();

            if (removeTarget.HasValue)
                service.RemoveMember(poolId, removeTarget.Value);
        }

        if (chanceSum > 100f)
            ImGui.TextColored(WarningColor, $"Explicit chances sum to {chanceSum:0.#}%\n(over 100%: the tail never rolls)");

        if (module.Pending.Count > 0)
        {
            if (ImGui.Button($"Add {module.Pending.Count} selected spawn{(module.Pending.Count > 1 ? "s" : "")} to this pool"))
            {
                service.AddToPool(poolId, module.CollectPending());
                module.Pending.Clear();
            }
        }
        else
        {
            ImGui.TextDisabled("Click unpooled spawns in the world to add them.");
        }

        if (changed)
            service.NotifyDetailsChanged(poolId);
    }

    // ------------------------------------------------------------ entry-wide members -------------

    private void DrawEntryMembers(PoolDetails d)
    {
        ImGui.SeparatorText($"Entry-wide members ({d.EntryMembers.Count})");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Every spawn of the entry belongs to the pool\n(pool_creature_template / pool_gameobject_template)");

        bool changed = false;
        var tableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
        if (d.EntryMembers.Count > 0 && ImGui.BeginTable("poolentrymembers", 3, tableFlags))
        {
            ImGui.TableSetupColumn("Entry");
            ImGui.TableSetupColumn("Chance", ImGuiTableColumnFlags.WidthFixed, 52);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 22);

            PoolEntryKey? removeTarget = null;
            foreach (var (entry, data) in d.EntryMembers.OrderBy(kv => kv.Key.Entry).ToList())
            {
                ImGui.TableNextRow();
                ImGui.PushID(entry.IsCreature ? (int)entry.Entry : -(int)entry.Entry);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{entry.Entry} {EntryName(entry)}");
                if (data.Description is { Length: > 0 } entryDescription && ImGui.IsItemHovered())
                    ImGui.SetTooltip(entryDescription);
                // an entry-pooled entry must not also have guid-pooled spawns - advisory only
                if (AnyGuidOfEntryPooled(entry))
                {
                    ImGui.SameLine();
                    ImGui.TextColored(WarningColor, "(!)");
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("A spawn of this entry is also pooled by guid -\nthe core rejects entries pooled both ways");
                }

                ImGui.TableNextColumn();
                float chance = data.Chance;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputFloat("##chance", ref chance, 0, 0, "%.1f"))
                {
                    var newData = data;
                    newData.Chance = ClampChance(chance, "Entry chance");
                    d.EntryMembers[entry] = newData;
                    changed = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Explicit roll chance in percent (0 = equal-chance member)");

                ImGui.TableNextColumn();
                if (ImGui.SmallButton("x"))
                    removeTarget = entry;
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove this entry from the pool (applied on Save)");

                ImGui.PopID();
            }
            ImGui.EndTable();

            if (removeTarget.HasValue)
                service.RemoveEntryMember(d.Id, removeTarget.Value);
        }

        if (ImGui.SmallButton(newEntryIsCreature ? "Creature" : "GameObject"))
            newEntryIsCreature = !newEntryIsCreature;
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Toggle the entry type to add");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(90);
        ImGui.InputInt("##newentry", ref newEntryId, 0, 0);
        entryPicker.PickButton("picknewentry", newEntryIsCreature, newEntryId, picked => newEntryId = (int)picked);
        ImGui.SameLine();
        if (ImGui.SmallButton("Add entry") && newEntryId > 0)
        {
            service.AddEntryMember(d.Id, new PoolEntryKey(newEntryIsCreature, (uint)newEntryId));
            newEntryId = 0;
        }

        if (changed)
            service.NotifyDetailsChanged(d.Id);
    }

    private string EntryName(PoolEntryKey entry) => entry.IsCreature
        ? cachedDatabase.GetCachedCreatureTemplate(entry.Entry)?.Name ?? ""
        : cachedDatabase.GetCachedGameObjectTemplate(entry.Entry)?.Name ?? "";

    /// <summary>True when any LOADED spawn of the entry is guid-pooled somewhere (a best-effort
    /// check over streamed-in spawns - unloaded areas can't be checked).</summary>
    private bool AnyGuidOfEntryPooled(PoolEntryKey entry)
    {
        foreach (var spawn in module.FindSpawnsOfEntry(entry))
        {
            if (service.PoolOf(new PoolMember(entry.IsCreature, spawn.Guid)) != null)
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ child pools --------------

    private void DrawChildPools(PoolDetails d)
    {
        service.CollectChildren(d.Id, children);
        if (children.Count == 0)
            return;
        children.Sort();

        ImGui.SeparatorText($"Child pools ({children.Count})");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Pools whose mother is this pool - each is one rollable member");

        foreach (var childId in children)
        {
            ImGui.PushID((int)childId);

            if (ImGui.SmallButton("x"))
            {
                service.TrySetMotherPool(childId, null);
                ImGui.PopID();
                break;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Detach the child (it becomes a top-level pool)");
            ImGui.SameLine();
            string name = service.PoolNames.TryGetValue(childId, out var n) ? n : "";
            if (ImGui.SmallButton($"{childId} {name}"))
                module.SelectedPoolId = childId; // jump into the child
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Edit this child pool");

            if (service.GetDetails(childId) is { } child)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(60);
                float chance = child.MotherChance;
                if (ImGui.InputFloat("##childchance", ref chance, 0, 0, "%.1f"))
                {
                    child.MotherChance = ClampChance(chance, "Child pool chance");
                    service.NotifyDetailsChanged(childId);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Roll chance of the child within this pool (0 = equal-chance)");
            }

            ImGui.PopID();
        }
    }

    // ------------------------------------------------------------------ delete pool --------------

    private void DrawDeletePool(uint poolId)
    {
        if (ImGui.Button("Delete pool...", new Vector2(-1, 0)))
            ImGui.OpenPopup("Delete spawn pool");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Deletes the pool with all its member and nesting rows.\nMember spawns stay in the world, just unpooled; child pools\nbecome top-level. Applied on Save.");

        bool open = true;
        if (!ImGuiEx.BeginPopupModal("Delete spawn pool", ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        string name = service.PoolNames.TryGetValue(poolId, out var n) ? n : "";
        ImGui.TextUnformatted($"Delete pool {poolId} \"{name}\"?");
        ImGui.TextDisabled("Member spawns stay in the world, just unpooled;\nchild pools become top-level.\nThe database rows are removed when you Save.");
        ImGui.Separator();
        if (ImGui.Button("Delete", new Vector2(120, 0)))
        {
            service.DeletePool(poolId);
            module.SelectedPoolId = 0;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }
}
