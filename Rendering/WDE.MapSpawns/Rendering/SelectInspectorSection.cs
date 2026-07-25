using Hexa.NET.ImGui;
using Prism.Events;
using TheEngine;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.CreatureLinking;
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
    private readonly ICreatureLinkEditorService creatureLinkService;
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
        ICreatureLinkEditorService creatureLinkService,
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
        this.creatureLinkService = creatureLinkService;
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

    /// <summary>A compact one-line editor jump: link-colored clickable text instead of a full-width
    /// button, so the many-jump sections stay scannable (one visual weight per section).</summary>
    private static void LinkRow(string text, string tooltip, Action open)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, EditorTheme.LinkText);
        bool clicked = ImGui.Selectable(text);
        ImGui.PopStyleColor();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
        if (clicked)
            open();
    }

    /// <summary>One-click jumps into the app-side editors for everything this spawn is made of:
    /// its own creature/gameobject row, its template, and the template's gossip menu.</summary>
    private void DrawEditorJumps(SpawnInstance spawn, CreatureSpawnInstance? creature, GameObjectSpawnInstance? go)
    {
        ImGui.SeparatorText("Editors");
        string noun = creature != null ? "creature" : "gameobject";

        if (spawnContextMenu is { } menu)
            LinkRow($"{Lucide.SquarePen} {noun} row (guid {spawn.Guid})##editrow",
                $"Opens this spawn's {noun} table row in the 1:1 editor\n(the spawn reloads from the database when it closes)",
                () => mainThread.Dispatch(() => menu.EditRowCommand.Execute(spawn)));

        LinkRow($"{Lucide.FileCog} {noun}_template {spawn.Entry}##edittemplate",
            "Opens the template editor for this entry in a new document\n(shared by every spawn of the entry)",
            () => eventAggregator.GetEvent<OpenTemplateEditorEvent>().Publish(new OpenTemplateEditorRequest
            {
                IsCreature = creature != null,
                Entry = spawn.Entry,
            }));

        if (creature != null && creature.CreatureTemplate.GossipMenuId != 0)
        {
            uint menuId = creature.CreatureTemplate.GossipMenuId;
            LinkRow($"{Lucide.MessageSquare} gossip menu {menuId}##editgossip",
                "Opens the gossip_menu editor for the template's gossip menu",
                () => eventAggregator.GetEvent<OpenGossipMenuEditorEvent>().Publish(menuId));
        }

        DrawLootJumps(spawn, creature, go);
        DrawRelatedTableJumps(spawn, creature, go);
    }

    private void DrawLootJumps(SpawnInstance spawn, CreatureSpawnInstance? creature, GameObjectSpawnInstance? go)
    {
        void LootRow(string label, string tooltip, LootSourceType type)
        {
            LinkRow(label, tooltip, () =>
                eventAggregator.GetEvent<OpenLootEditorEvent>().Publish(new OpenLootEditorRequest
                {
                    Type = type,
                    Entry = spawn.Entry,
                }));
        }

        if (creature != null)
        {
            LootRow($"{Lucide.Gem} loot##lootkill", "Opens the loot editor for this creature's kill loot", LootSourceType.Creature);
            if (creature.CreatureTemplate.SkinningLootId != 0)
                LootRow($"{Lucide.Gem} skinning loot##lootskin", "Opens the loot editor for this creature's skinning loot", LootSourceType.Skinning);
            if (creature.CreatureTemplate.PickpocketLootId != 0)
                LootRow($"{Lucide.Gem} pickpocket loot##lootpick", "Opens the loot editor for this creature's pickpocketing loot", LootSourceType.Pickpocketing);
        }
        else if (go != null && go.GameObjectTemplate.GetLootId() != null)
        {
            LootRow($"{Lucide.Gem} loot##lootgo", "Opens the loot editor for this gameobject's chest/gathering loot", LootSourceType.GameObject);
        }
    }

    /// <summary>Entry-keyed side tables (vendor stock, trainer spells, spellclick, quest relations).
    /// Buttons show by npcflag / GO type; the bridge resolves the semantic kind to the active core's
    /// table name and opens the generic editor filtered to the entry.</summary>
    private void DrawRelatedTableJumps(SpawnInstance spawn, CreatureSpawnInstance? creature,
        GameObjectSpawnInstance? go)
    {
        void TableButton(string label, string tooltip, SpawnRelatedTable table)
        {
            LinkRow(label, tooltip, () =>
                eventAggregator.GetEvent<OpenSpawnRelatedTableEvent>().Publish(new OpenSpawnRelatedTableRequest
                {
                    Table = table,
                    IsCreature = creature != null,
                    Entry = spawn.Entry,
                }));
        }

        const GameDefines.NpcFlags vendorMask = GameDefines.NpcFlags.Vendor | GameDefines.NpcFlags.VendorAmmo |
                                                GameDefines.NpcFlags.VendorFood | GameDefines.NpcFlags.VendorPoison |
                                                GameDefines.NpcFlags.VendorReagent;
        const GameDefines.NpcFlags trainerMask = GameDefines.NpcFlags.Trainer | GameDefines.NpcFlags.TrainerClass |
                                                 GameDefines.NpcFlags.TrainerProfession;

        var npcFlags = creature?.CreatureTemplate.NpcFlags ?? 0;
        if (creature != null)
        {
            if ((npcFlags & vendorMask) != 0)
                TableButton($"{Lucide.ShoppingCart} vendor items##related", "Opens the vendor stock table filtered to this entry", SpawnRelatedTable.Vendor);
            if ((npcFlags & trainerMask) != 0)
                TableButton($"{Lucide.GraduationCap} trainer spells##related", "Opens the trainer spell list filtered to this entry", SpawnRelatedTable.Trainer);
            if ((npcFlags & GameDefines.NpcFlags.SpellClick) != 0)
                TableButton($"{Lucide.MousePointerClick} spellclick spells##related", "Opens the spellclick spells table filtered to this entry", SpawnRelatedTable.SpellClick);
        }

        bool questGiver = creature != null
            ? (npcFlags & GameDefines.NpcFlags.QuestGiver) != 0
            : go != null && go.GameObjectTemplate.Type == GameobjectType.QuestGiver;
        if (questGiver)
        {
            TableButton($"{Lucide.CircleAlert} started quests (!)##related", "Which quests this spawn's entry offers\n(quest starter relation table)", SpawnRelatedTable.QuestStarter);
            TableButton($"{Lucide.CircleHelp} ended quests (?)##related", "Which quests this spawn's entry accepts turn-ins for\n(quest ender relation table)", SpawnRelatedTable.QuestEnder);
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
        bool markerDrawn = false;
        // the resolver only reads the terrain (ADT) area ids - inside WMOs (buildings, caves,
        // instances) the id belongs to the terrain underneath, not the interior area; the note
        // rides a (?) on the first row instead of permanently occupying panel space
        const string interiorNote = "Interior areas can't be detected - inside buildings\nand caves this is the terrain area underneath.";
        if (area.HasValue)
        {
            InfoRow("Area", $"{WorldPointNames.AreaName(dbc, (uint)area.Value)} ({area.Value})");
            EditorTheme.HelpMarker(interiorNote);
            markerDrawn = true;
        }
        if (zone.HasValue)
        {
            InfoRow("Zone", $"{WorldPointNames.AreaName(dbc, (uint)zone.Value)} ({zone.Value})");
            if (!markerDrawn)
                EditorTheme.HelpMarker(interiorNote);
        }

        // spell_area.area accepts a subarea id just as well as a zone id (the column name lies),
        // so each gets its own filtered editor jump
        if (area.HasValue)
            SpellAreaRow("area", (uint)area.Value, dbc);
        if (zone.HasValue && zone != area)
            SpellAreaRow("zone", (uint)zone.Value, dbc);
    }

    private void SpellAreaRow(string kind, uint id, DbcManager dbc)
    {
        LinkRow($"{Lucide.Sparkles} spell_area: {WorldPointNames.AreaName(dbc, id)}##spellarea_{kind}",
            $"Opens the spell_area table editor filtered to this {kind} ({id})\n(spells auto-applied/allowed while in it)",
            () => eventAggregator.GetEvent<OpenSpellAreaEditorEvent>().Publish(new OpenSpellAreaEditorRequest
            {
                AreaId = (int)id,
                ZoneId = 0, // exact filter - each row targets its own id
            }));
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
            // a C++ ScriptName owns this spawn's behavior - creating an EventAI/dbscript for it
            // would silently never run, so the create offers disable instead of misleading
            bool cppScriptBlocks = !string.IsNullOrEmpty(scriptName);
            ImGui.TextDisabled("Create new:");
            ImGui.BeginDisabled(cppScriptBlocks);
            for (int i = 0; i < state.Slots.Count; ++i)
            {
                var slot = state.Slots[i];
                if (!slot.CanOpen || slot.Exists)
                    continue;
                ImGui.SameLine();
                if (ImGui.SmallButton($"+ {slot.Name}##script{i}"))
                    scriptsService.Open(owner, i);
                if (cppScriptBlocks && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    ImGui.SetTooltip($"This spawn is scripted in C++ (\"{scriptName}\") -\nscripts are mutually exclusive");
            }
            ImGui.EndDisabled();
        }

        // scripts are edited in their own documents - offer a re-check after coming back
        if (ImGui.SmallButton($"{Lucide.RefreshCw} Refresh##scripts"))
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
            MembershipRow($"{Lucide.Boxes} Group", $"{groupId} {name}", "##editgroup", () =>
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
                MembershipRow($"{Lucide.Users} Formation", $"follows leader {asMember.LeaderGuid}", "##editformation", () =>
                {
                    formationService.Selected = asMember;
                    toolService.ActiveTool = SpawnEditorTool.Formation;
                });
                any = true;
            }
            if (leads > 0)
            {
                MembershipRow($"{Lucide.Users} Formation", $"leads {leads} creature{(leads > 1 ? "s" : "")}", "##editformationlead",
                    () => toolService.ActiveTool = SpawnEditorTool.Formation);
                any = true;
            }
        }

        // creature linking (creatures only): the per-guid creature_linking row this creature is the
        // slave or a master of, plus the entry-wide creature_linking_template rows on this map
        if (creature != null && creatureLinkService.IsSupported)
        {
            EditableCreatureLink? asSlave = null;
            int slaves = 0;
            foreach (var link in creatureLinkService.GuidLinks)
            {
                if (link.Guid == creature.Guid)
                    asSlave = link;
                if (link.MasterGuid == creature.Guid)
                    slaves++;
            }

            if (asSlave != null)
            {
                MembershipRow($"{Lucide.Link} Link", $"slave of guid {asSlave.MasterGuid}", "##editlinkslave", () =>
                {
                    creatureLinkService.Selected = asSlave;
                    toolService.ActiveTool = SpawnEditorTool.CreatureLink;
                });
                any = true;
            }
            if (slaves > 0)
            {
                MembershipRow($"{Lucide.Link} Link", $"master of {slaves} creature{(slaves > 1 ? "s" : "")}", "##editlinkmaster",
                    () => toolService.ActiveTool = SpawnEditorTool.CreatureLink);
                any = true;
            }

            EditableCreatureLinkTemplate? entryAsSlave = null;
            int entrySlaves = 0;
            foreach (var link in creatureLinkService.TemplateLinks)
            {
                if (link.Entry == creature.Entry)
                    entryAsSlave = link;
                if (link.MasterEntry == creature.Entry)
                    entrySlaves++;
            }

            if (entryAsSlave != null)
            {
                MembershipRow($"{Lucide.Link2} Entry link", $"slave of entry {entryAsSlave.MasterEntry}", "##editentrylinkslave", () =>
                {
                    creatureLinkService.Selected = entryAsSlave;
                    toolService.ActiveTool = SpawnEditorTool.CreatureLink;
                });
                any = true;
            }
            if (entrySlaves > 0)
            {
                MembershipRow($"{Lucide.Link2} Entry link", $"master of {entrySlaves} entr{(entrySlaves > 1 ? "ies" : "y")}", "##editentrylinkmaster",
                    () => toolService.ActiveTool = SpawnEditorTool.CreatureLink);
                any = true;
            }
        }

        // waypoint path (creatures only)
        bool canAddPath = false;
        if (creature != null && waypointService.SupportsCreaturePaths)
        {
            if (waypointService.ResolveCreaturePath(creature) is { } attached)
            {
                MembershipRow($"{Lucide.Route} Path", $"{attached.source.ToName()} #{attached.key}", "##editpath",
                    () => waypointService.RequestEditCreaturePath(creature));
                any = true;
            }
            else
                canAddPath = true;
        }

        // the entry-shared creature_movement_template path (CMaNGOS), separate from the per-guid
        // one - only a membership when the entry really has rows (or one was just created in-editor)
        bool canAddTemplatePath = false;
        if (creature != null && waypointService.SupportsCreatureTemplatePaths)
        {
            bool hasUnsavedNew = waypointService.FindLoaded(WaypointSource.MangosCreatureMovementTemplate, creature.Entry) != null;
            bool? hasInDb = waypointService.HasCreatureTemplatePath(creature.Entry);
            if (hasInDb == true || hasUnsavedNew)
            {
                MembershipRow($"{Lucide.Route} Template path", "creature_movement_template", "##edittemplatepath",
                    () => waypointService.RequestEditCreatureTemplatePath(creature!));
                any = true;
            }
            else if (hasInDb == false)
                canAddTemplatePath = true;
            // hasInDb == null: the existence check is still in flight - show neither yet
        }

        if (!any)
            ImGui.TextDisabled("None.");
        if (canAddPath && ImGui.SmallButton($"{Lucide.Plus} Add waypoint path"))
            waypointService.RequestEditCreaturePath(creature!);
        if (canAddTemplatePath && ImGui.SmallButton($"{Lucide.Plus} Add template path"))
            waypointService.RequestEditCreatureTemplatePath(creature!);
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
