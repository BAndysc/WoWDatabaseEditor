using Hexa.NET.ImGui;
using Prism.Events;
using TheEngine;
using TheMaths;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Formations;
using WDE.MapSpawns.Models.Solution;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.Models.Waypoints;
using WDE.MapSpawns.Rendering.WorldPoints;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// The Select tool's inspector: the selected spawn's identity/transform (replaces the old floating
/// "Selected spawn" window) plus its memberships - spawn group, formation link, attached waypoint
/// path - each with a one-click jump that switches to the relevant tool.
/// </summary>
public class SelectInspectorSection : IInspectorSection
{
    private readonly ISpawnSelectionService selectionService;
    private readonly ISpawnEditorToolService toolService;
    private readonly ISpawnGroupEditorService spawnGroupService;
    private readonly IFormationEditorService formationService;
    private readonly IWaypointEditorService waypointService;
    private readonly ISpawnScriptsService scriptsService;
    private readonly IWorldSpawnEditService editService;
    private readonly Engine engine;
    private readonly IGameContext gameContext;
    private readonly IEventAggregator eventAggregator;
    private readonly IMainThread mainThread;

    // the section is a plain container-constructed class, so injecting the visualizer would yield
    // a second transient instance - SpawnViewer attaches ITS instance (same pattern as SpawnPicker)
    private SpawnGroupVisualizer? spawnGroupVisualizer;

    // SpawnViewer's dragger - its CommitTransform is the one shared persist path (undo op +
    // formation-member follow), the numeric fields below commit through it
    private SpawnDragger? spawnDragger;
    private bool transformEdited;

    public SelectInspectorSection(ISpawnSelectionService selectionService,
        ISpawnEditorToolService toolService,
        ISpawnGroupEditorService spawnGroupService,
        IFormationEditorService formationService,
        IWaypointEditorService waypointService,
        ISpawnScriptsService scriptsService,
        IWorldSpawnEditService editService,
        Engine engine,
        IGameContext gameContext,
        IEventAggregator eventAggregator,
        IMainThread mainThread)
    {
        this.selectionService = selectionService;
        this.toolService = toolService;
        this.spawnGroupService = spawnGroupService;
        this.formationService = formationService;
        this.waypointService = waypointService;
        this.scriptsService = scriptsService;
        this.editService = editService;
        this.engine = engine;
        this.gameContext = gameContext;
        this.eventAggregator = eventAggregator;
        this.mainThread = mainThread;
    }

    public void AttachGroupVisualizer(SpawnGroupVisualizer visualizer) => spawnGroupVisualizer = visualizer;
    public void AttachDragger(SpawnDragger dragger) => spawnDragger = dragger;
    public void AttachPlacement(SpawnPlacementController controller) => placement = controller;
    // SpawnViewer's instance - its EditRow reload queue is the one SpawnViewer pumps
    public void AttachContextMenu(SpawnContextMenu menu) => spawnContextMenu = menu;

    private SpawnPlacementController? placement;
    private SpawnContextMenu? spawnContextMenu;

    public string Title => "Selection";

    public bool IsDirty => editService.IsAvailable && editService.HasChanges;

    public Func<Task>? SaveSelf => IsDirty ? SaveSpawnEdits : null;

    // the bridge toasts the save outcome itself (the save is event-driven and async)
    public bool SaveReportsItself => true;

    private Task SaveSpawnEdits()
    {
        editService.Save();
        return Task.CompletedTask;
    }

    public string? Hints
    {
        get
        {
            // placement mode owns the pointer - its status is THE thing to communicate
            if (placement is { IsPlacing: true, StatusHint: { } placingHint })
                return placingHint;

            // a drag in flight: say what click/Esc/modifiers do RIGHT NOW
            if (spawnDragger?.DragHint is { } dragHint)
                return dragHint;

            if (selectionService.SelectedSpawn.Value is not { IsSpawned: true } sel)
                return "Click a spawn to select · right-click ground: add creature/gameobject · Shift+A add · F1 all shortcuts";
            // Del is a toggle - say what it will actually do to THIS spawn
            return editService.IsPendingDelete(sel is CreatureSpawnInstance, sel.Guid)
                ? "Marked for deletion · Del restore · Save applies · G grab · R rotate"
                : "G grab · R rotate · F fly to · Del delete · Ctrl+D duplicate · double-click: edit row · F1 all shortcuts";
        }
    }

    public void DrawContent()
    {
        if (selectionService.SelectedSpawn.Value is not { IsSpawned: true } spawn)
        {
            ImGui.TextDisabled("Nothing selected.");
            ImGui.TextDisabled("Click a spawn in the world.");
            DrawAreaSection();
            return;
        }

        var creature = spawn as CreatureSpawnInstance;
        var go = spawn as GameObjectSpawnInstance;

        if (creature != null)
            ImGui.TextColored(EditorTheme.SelectionGold, creature.CreatureTemplate.Name);
        else if (go != null)
            ImGui.TextColored(EditorTheme.SelectionGold, go.GameObjectTemplate.Name);

        ImGui.TextDisabled($"{(creature != null ? "Creature" : "GameObject")} {spawn.Entry}  ·  guid {spawn.Header}");

        // unsaved state must be legible right where the user is looking (matches the world dither)
        if (editService.IsPendingDelete(creature != null, spawn.Guid))
        {
            ImGui.TextColored(EditorTheme.DangerText, "Marked for deletion");
            ImGui.TextDisabled("Save deletes it from the database - Del restores it.");
        }
        else if (editService.NewSpawns.Any(n => n.IsCreature == (creature != null) && n.Guid == spawn.Guid))
        {
            ImGui.TextColored(EditorTheme.Warning, "Not saved yet");
            ImGui.TextDisabled("Save inserts it into the database.");
        }

        DrawTransform(spawn, creature, go);

        DrawEditorJumps(spawn, creature, go);
        DrawMemberships(spawn, creature);
        DrawScripts(spawn, creature, go);
        DrawAreaSection();
    }

    /// <summary>One-click jumps into the app-side editors for everything this spawn is made of:
    /// its own creature/gameobject row, its template, and the template's gossip menu.</summary>
    private void DrawEditorJumps(SpawnInstance spawn, CreatureSpawnInstance? creature, GameObjectSpawnInstance? go)
    {
        ImGui.SeparatorText("Editors");
        var fullWidth = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 0);
        string noun = creature != null ? "creature" : "gameobject";

        if (spawnContextMenu is { } menu)
        {
            if (ImGui.Button($"Edit {noun} row (guid {spawn.Guid})", fullWidth))
                mainThread.Dispatch(() => menu.EditRowCommand.Execute(spawn));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"Opens this spawn's {noun} table row in the 1:1 editor\n(the spawn reloads from the database when it closes)");
        }

        if (ImGui.Button($"Edit {noun}_template {spawn.Entry}", fullWidth))
            eventAggregator.GetEvent<OpenTemplateEditorEvent>().Publish(new OpenTemplateEditorRequest
            {
                IsCreature = creature != null,
                Entry = spawn.Entry,
            });
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Opens the template editor for this entry in a new document\n(shared by every spawn of the entry)");

        if (creature != null && creature.CreatureTemplate.GossipMenuId != 0)
        {
            uint menuId = creature.CreatureTemplate.GossipMenuId;
            if (ImGui.Button($"Edit gossip menu {menuId}", fullWidth))
                eventAggregator.GetEvent<OpenGossipMenuEditorEvent>().Publish(menuId);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Opens the gossip_menu editor for the template's gossip menu");
        }
    }

    private SpawnInstance? lastTransformSpawn;

    /// <summary>Editable position/orientation: drag to nudge (live world update), Ctrl+click to type
    /// exact values (e.g. from a sniff). Releasing/leaving the field commits ONE undoable spawn move
    /// through the shared dragger path (formation members follow a leader edit). Falls back to
    /// read-only labels when spawn editing isn't available (headless hosts).</summary>
    private void DrawTransform(SpawnInstance spawn, CreatureSpawnInstance? creature, GameObjectSpawnInstance? go)
    {
        var worldObject = spawn.WorldObject!;

        if (!ReferenceEquals(spawn, lastTransformSpawn))
        {
            lastTransformSpawn = spawn;
            transformEdited = false; // an in-flight edit never leaks onto a newly selected spawn
        }

        if (!editService.IsAvailable || spawnDragger == null)
        {
            var position = worldObject.Position;
            ImGui.LabelText("Position", $"{position.X:0.##}, {position.Y:0.##}, {position.Z:0.##}");
            if (creature?.Creature != null)
                ImGui.LabelText("Orientation", creature.Creature.Orientation.ToString("0.###") + " rad");
            else if (go?.GameObject != null)
            {
                var euler = go.GameObject.Rotation.ToEulerDeg();
                ImGui.LabelText("Orientation", go.GameObject.Orientation.ToString("0.###") + " rad");
                ImGui.LabelText("Rotation", $"{euler.X:0.#}° {euler.Y:0.#}° {euler.Z:0.#}°");
            }
            return;
        }

        bool commit = false;

        var numPos = new System.Numerics.Vector3(worldObject.Position.X, worldObject.Position.Y, worldObject.Position.Z);
        if (ImGui.DragFloat3("Position", ref numPos, 0.05f, 0f, 0f, "%.2f"))
        {
            worldObject.Position = new Vector3(numPos.X, numPos.Y, numPos.Z);
            transformEdited = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Drag to nudge, Ctrl+click to type exact coordinates.\nOne undo step per edit, persisted on Save.");
        commit |= ImGui.IsItemDeactivatedAfterEdit();

        float orientation = creature?.Creature?.Orientation ?? go?.GameObject?.Orientation ?? 0f;
        if (ImGui.DragFloat("Orientation", ref orientation, 0.01f, 0f, 0f, "%.3f rad"))
        {
            if (creature?.Creature != null)
                creature.Creature.Orientation = orientation;
            else if (go?.GameObject != null)
                go.GameObject.Rotation = Quaternion.CreateFromAxisAngle(Vectors.Up, orientation);
            transformEdited = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(go != null
                ? $"Yaw in radians ({orientation * 180f / MathF.PI:0.#}°) - drag to nudge, Ctrl+click to type.\nSetting it resets any gizmo tilt to a pure yaw rotation."
                : $"Yaw in radians ({orientation * 180f / MathF.PI:0.#}°) - drag to nudge, Ctrl+click to type.");
        commit |= ImGui.IsItemDeactivatedAfterEdit();

        if (go?.GameObject != null)
        {
            var euler = go.GameObject.Rotation.ToEulerDeg();
            ImGui.LabelText("Rotation", $"{euler.X:0.#}° {euler.Y:0.#}° {euler.Z:0.#}°");
        }

        if (commit && transformEdited)
        {
            transformEdited = false;
            spawnDragger.CommitTransform(spawn);
        }
    }

    /// <summary>The zone/area under the camera, with per-area editor jumps (currently spell_area,
    /// opened as the generic table editor filtered to this area + its zone via the bridge).</summary>
    private void DrawAreaSection()
    {
        var dbc = gameContext.DbcManager;
        var cameraPos = engine.CameraManager.MainCamera.Transform.Position;
        var (zone, area) = WorldPointNames.ZoneAndArea(dbc, gameContext.ZoneAreaManager, gameContext.CurrentMapId, cameraPos);
        if (zone == null && area == null)
            return;

        ImGui.SeparatorText("Area");
        if (area.HasValue)
            InfoRow("Area", $"{WorldPointNames.AreaName(dbc, (uint)area.Value)} ({area.Value})");
        if (zone.HasValue)
            InfoRow("Zone", $"{WorldPointNames.AreaName(dbc, (uint)zone.Value)} ({zone.Value})");
        // the resolver only reads the terrain (ADT) area ids - inside WMOs (buildings, caves,
        // instances) the id belongs to the terrain underneath, not the interior area
        ImGui.PushFont(default, ImGui.GetFontSize() * 0.85f);
        ImGui.TextDisabled("Interior areas can't be detected - inside buildings and caves\nthis is the terrain area underneath.");
        ImGui.PopFont();

        // spell_area.area accepts a subarea id just as well as a zone id (the column name lies),
        // so each gets its own filtered editor jump
        if (area.HasValue)
            SpellAreaButton("area", (uint)area.Value, dbc);
        if (zone.HasValue && zone != area)
            SpellAreaButton("zone", (uint)zone.Value, dbc);
    }

    private void SpellAreaButton(string kind, uint id, DbcManager dbc)
    {
        if (ImGui.Button($"Edit spell_area for {WorldPointNames.AreaName(dbc, id)}##spellarea_{kind}",
                new Vector2(ImGui.GetContentRegionAvail().X, 0)))
        {
            eventAggregator.GetEvent<OpenSpellAreaEditorEvent>().Publish(new OpenSpellAreaEditorRequest
            {
                AreaId = (int)id,
                ZoneId = 0, // exact filter - each button targets its own id
            });
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Opens the spell_area table editor filtered to this {kind} ({id})\n(spells auto-applied/allowed while in it)");
    }

    private void DrawScripts(SpawnInstance spawn, CreatureSpawnInstance? creature, GameObjectSpawnInstance? go)
    {
        var aiName = creature?.CreatureTemplate.AIName ?? go?.GameObjectTemplate.AIName;
        var scriptName = creature?.CreatureTemplate.ScriptName ?? go?.GameObjectTemplate.ScriptName;
        var owner = new SpawnScriptOwner(creature != null, spawn.Entry, spawn.Guid);
        var state = scriptsService.GetState(owner);

        // headless hosts have no scripts bridge - hide the whole section (unless there is
        // template info worth showing anyway)
        bool haveTemplateInfo = !string.IsNullOrEmpty(aiName) || !string.IsNullOrEmpty(scriptName);
        if (!scriptsService.IsAvailable && !haveTemplateInfo)
            return;

        ImGui.SeparatorText("Scripts");

        // local template facts, no round-trip needed
        if (!string.IsNullOrEmpty(aiName))
            InfoRow("AI", aiName!);
        if (!string.IsNullOrEmpty(scriptName))
            InfoRow("C++ script", scriptName!);

        if (!scriptsService.IsAvailable)
            return;
        if (state == null)
        {
            ImGui.TextDisabled("Loading...");
            return;
        }

        if (state.Slots.Count == 0 && !haveTemplateInfo)
            ImGui.TextDisabled("No script slots on this core.");

        // attached scripts first, then the "create" offers for the empty slots
        var fullWidth = new Vector2(ImGui.GetContentRegionAvail().X, 0);
        bool anyMissing = false;
        for (int i = 0; i < state.Slots.Count; ++i)
        {
            var slot = state.Slots[i];
            if (!slot.CanOpen)
                InfoRow(slot.Name, slot.Exists ? slot.Detail ?? "yes" : "none");
            else if (slot.Exists)
            {
                var detail = slot.Detail != null ? $" ({slot.Detail})" : "";
                if (ImGui.Button($"Open {slot.Name}{detail}##script{i}", fullWidth))
                    scriptsService.Open(owner, i);
            }
            else
                anyMissing = true;
        }

        if (anyMissing)
        {
            ImGui.TextDisabled("Create new:");
            for (int i = 0; i < state.Slots.Count; ++i)
            {
                var slot = state.Slots[i];
                if (!slot.CanOpen || slot.Exists)
                    continue;
                if (ImGui.Button($"+ {slot.Name}##script{i}", fullWidth))
                    scriptsService.Open(owner, i);
            }
        }

        // scripts are edited in their own documents - offer a re-check after coming back
        if (ImGui.SmallButton("Refresh##scripts"))
            scriptsService.Refresh(owner);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Re-check which scripts exist for this spawn\n(after saving a script in its own editor, the buttons here may be stale)");
    }

    private static void InfoRow(string label, string value)
    {
        ImGui.TextDisabled(label + ":");
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
    }

    private void DrawMemberships(SpawnInstance spawn, CreatureSpawnInstance? creature)
    {
        ImGui.SeparatorText("Memberships");
        bool any = false;

        // spawn group
        var member = new SpawnGroupMember(creature != null, spawn.Guid);
        if (spawnGroupService.IsSupported && spawnGroupService.GroupOf(member) is { } groupId)
        {
            var name = spawnGroupService.GroupNames.TryGetValue(groupId, out var n) ? n : "";
            MembershipRow("Group", $"{groupId} {name}", "##editgroup", () =>
            {
                spawnGroupService.RequestedEditGroup = groupId;
                toolService.ActiveTool = SpawnEditorTool.SpawnGroup;
            });
            if (spawnGroupVisualizer?.CapNotice is { } capNotice)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.75f, 0.3f, 1f));
                ImGui.TextWrapped(capNotice);
                ImGui.PopStyleColor();
            }
            any = true;
        }

        // formation (creatures only)
        if (creature != null && formationService.IsSupported)
        {
            EditableFormation? asMember = null;
            int leads = 0;
            foreach (var f in formationService.LoadedFormations)
            {
                if (f.IsLeaderSelfRow)
                    continue;
                if (f.MemberGuid == creature.Guid)
                    asMember = f;
                if (f.LeaderGuid == creature.Guid)
                    leads++;
            }

            if (asMember != null)
            {
                MembershipRow("Formation", $"follows leader {asMember.LeaderGuid}", "##editformation", () =>
                {
                    formationService.Selected = asMember;
                    toolService.ActiveTool = SpawnEditorTool.Formation;
                });
                any = true;
            }
            if (leads > 0)
            {
                MembershipRow("Formation", $"leads {leads} creature{(leads > 1 ? "s" : "")}", "##editformationlead",
                    () => toolService.ActiveTool = SpawnEditorTool.Formation);
                any = true;
            }
        }

        // waypoint path (creatures only)
        bool canAddPath = false;
        if (creature != null && waypointService.SupportsCreaturePaths)
        {
            if (waypointService.ResolveCreaturePath(creature) is { } attached)
            {
                MembershipRow("Path", $"{attached.source.ToName()} #{attached.key}", "##editpath",
                    () => waypointService.RequestEditCreaturePath(creature));
                any = true;
            }
            else
                canAddPath = true;
        }

        // the entry-shared creature_movement_template path (CMaNGOS), separate from the per-guid one
        bool canEditTemplate = creature != null && waypointService.SupportsCreatureTemplatePaths;
        if (canEditTemplate)
        {
            MembershipRow("Template path", "creature_movement_template", "##edittemplatepath",
                () => waypointService.RequestEditCreatureTemplatePath(creature!));
            any = true;
        }

        if (!any)
            ImGui.TextDisabled("None.");
        if (canAddPath && ImGui.SmallButton("Add waypoint path"))
            waypointService.RequestEditCreaturePath(creature!);
    }

    private static void MembershipRow(string label, string value, string editId, Action edit)
    {
        ImGui.TextDisabled(label + ":");
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
        ImGui.SameLine();
        if (ImGui.SmallButton($"Edit{editId}"))
            edit();
    }
}
