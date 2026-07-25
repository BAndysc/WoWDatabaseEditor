using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Hexa.NET.ImGui;
using TheEngine;
using TheMaths;
using WDE.Common;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.AreaTriggers;
using WDE.MapSpawns.Models.CreatureLinking;
using WDE.MapSpawns.Models.Formations;
using WDE.MapSpawns.Models.Pools;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.Models.Waypoints;
using WDE.MapSpawns.Models.WorldPoints;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// The game-view tool selector: an icon strip drawn INSIDE the "3D" view (appended to the engine's
/// game-view window, no separate floating window). One exclusive active tool (Select / Waypoint /
/// Formation / Spawn group); each editor module only interacts with the world while its tool is
/// active. The strip reports its rect via <see cref="TheEngine.Managers.IEngineView.BlockClicksOver"/>
/// so clicks on it never fall through into the world. Rendered from <see cref="SpawnViewer.RenderGUI"/>.
/// Also the <see cref="WDE.MapRenderer.Managers.ISavable"/> behind the 3D document: it aggregates
/// every editor's dirty flag into IsModified (tab asterisk) and serves the document's Save command.
/// </summary>
public class GameViewToolbar : WDE.MapRenderer.Managers.ISavable
{
    private readonly Engine engine;
    private readonly ISpawnEditorToolService toolService;
    private readonly IFormationEditorService formationService;
    private readonly ISpawnGroupEditorService spawnGroupService;
    private readonly IPoolEditorService poolService;
    private readonly IWaypointEditorService waypointService;
    private readonly ISafeLocEditorService safeLocService;
    private readonly ISpellTargetEditorService spellTargetService;
    private readonly ICreatureLinkEditorService linkService;
    private readonly IAreaTriggerEditorService areaTriggerService;
    private readonly IWorldSpawnEditService editService;
    private readonly IGameNotificationService notifications;
    private readonly SpawnEditorTutorial tutorial;

    private const float ButtonSize = 28f;
    private static readonly Vector4 ActiveColor = EditorTheme.Accent;
    private static readonly Vector4 UnsavedColor = EditorTheme.Warning;

    // the running SaveAll - while incomplete the Save button is disabled (no overlapping saves)
    private Task? saveTask;

    // attached by SpawnViewer after construction (the keymap is built alongside the toolbar)
    private SpawnEditorKeymap? keymap;

    public void AttachKeymap(SpawnEditorKeymap keymap) => this.keymap = keymap;

    // ToolButton is drawn for every tool every frame; composing the tooltip with string interpolation
    // each time allocated ~1MB/session. The two variants (idle/active) are constant per tool, so cache them.
    private readonly Dictionary<SpawnEditorTool, (string idle, string active)> tooltipCache = new();

    public GameViewToolbar(Engine engine,
        ISpawnEditorToolService toolService,
        IFormationEditorService formationService,
        ISpawnGroupEditorService spawnGroupService,
        IPoolEditorService poolService,
        IWaypointEditorService waypointService,
        ISafeLocEditorService safeLocService,
        ISpellTargetEditorService spellTargetService,
        ICreatureLinkEditorService linkService,
        IAreaTriggerEditorService areaTriggerService,
        IWorldSpawnEditService editService,
        IGameNotificationService notifications,
        SpawnEditorTutorial tutorial)
    {
        this.tutorial = tutorial;
        this.engine = engine;
        this.toolService = toolService;
        this.formationService = formationService;
        this.spawnGroupService = spawnGroupService;
        this.poolService = poolService;
        this.waypointService = waypointService;
        this.safeLocService = safeLocService;
        this.spellTargetService = spellTargetService;
        this.linkService = linkService;
        this.areaTriggerService = areaTriggerService;
        this.editService = editService;
        this.notifications = notifications;
    }

    public void RenderGUI()
    {
        // append into the engine's game-view window (drawn earlier this frame) - the strip lives
        // in the 3D view itself and moves/hides with it
        if (!ImGui.Begin("3D"))
        {
            ImGui.End();
            return;
        }

        var dl = ImGui.GetWindowDrawList();
        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1); // buttons first; the backdrop is drawn under them afterwards

        var contentTop = ImGui.GetCursorStartPos();
        ImGui.SetCursorPos(contentTop + new Vector2(10, 10));

        ImGui.BeginGroup();

        ToolButton("##tool_select", ToolIcon.Select, SpawnEditorTool.Select, true,
            "Select - click a spawn, G to grab, Delete to remove,\nright click empty ground to add a new spawn", 1);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_waypoint", ToolIcon.Waypoint, SpawnEditorTool.Waypoint, true,
            "Waypoints - pen tool: click terrain to append points to the selected path", 2);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_formation", ToolIcon.Formation, SpawnEditorTool.Formation, formationService.IsSupported,
            formationService.IsSupported ? "Formations - drag a creature onto its leader to link them"
                : spawnGroupService is { IsSupported: true, SupportsFormations: true }
                    ? "Formations - on this core formations are part of spawn groups;\nuse the Spawn group tool (formation shape + slots)"
                    : "Formations - not supported for this database core", 3);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_group", ToolIcon.SpawnGroup, SpawnEditorTool.SpawnGroup, spawnGroupService.IsSupported,
            spawnGroupService.IsSupported
                ? "Spawn groups - bundle spawns that (re)spawn together;\npick a group in the panel, then click spawns in the world to add members"
                : "Spawn groups - not supported for this database core", 4);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_pool", ToolIcon.Pool, SpawnEditorTool.Pool, poolService.IsSupported,
            poolService.IsSupported ? "Spawn pools - only some members of a pool are spawned at once"
                : "Spawn pools - not supported for this database core", 5);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_graveyard", ToolIcon.Graveyard, SpawnEditorTool.Graveyard, safeLocService.IsSupported,
            safeLocService.IsSupported ? "Graveyards - world safe locs and their death links"
                : "Graveyards - not supported for this database core", 6);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_spelltarget", ToolIcon.SpellTarget, SpawnEditorTool.SpellTarget, spellTargetService.IsSupported,
            spellTargetService.IsSupported ? "Spell target positions - destinations of DB-targeted spells (teleports)"
                : "Spell target positions - not supported for this database core", 7);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_creaturelink", ToolIcon.CreatureLink, SpawnEditorTool.CreatureLink, linkService.IsSupported,
            linkService.IsSupported ? "Creature linking - drag a creature onto another to link slave -> master\n(guid or entry mode in the panel)"
                : "Creature linking - not supported for this database core", 8);
        ImGui.SameLine(0, 4);
        ToolButton("##tool_areatrigger", ToolIcon.AreaTrigger, SpawnEditorTool.AreaTrigger, areaTriggerService.IsSupported,
            areaTriggerService.IsSupported ? "Area triggers - click a trigger shape to edit its teleport\ndestination, tavern flag, exploration quest and script"
                : "Area triggers - not supported for this database core", 9);

        VerticalSeparator(dl);
        GizmoModeDropdown(dl);

        // no Save button here: the document's own save icon (and Ctrl+S) saves the 3D editors -
        // the toolbar's ISavable side feeds it and drives the tab's modified asterisk

        // undo/redo only cover spawn edits (the bridge's op stack) - the tooltip names the exact
        // operation so it never looks like it would undo e.g. the waypoint edit made a second ago
        {
            VerticalSeparator(dl);
            var (undoTooltip, redoTooltip) = UndoRedoTooltips();
            ImGui.SameLine(0, 4);
            if (ToolIcons.IconButton("##undo", ToolIcon.Undo, ButtonSize, false, undoTooltip, editService.CanUndo))
                editService.Undo();
            ImGui.SameLine(0, 4);
            if (ToolIcons.IconButton("##redo", ToolIcon.Redo, ButtonSize, false, redoTooltip, editService.CanRedo))
                editService.Redo();
        }

        VerticalSeparator(dl);
        ImGui.SameLine(0, 4);
        if (ToolIcons.IconButton("##palette", ToolIcon.Search, ButtonSize, false,
                "Command palette - every editor action, searchable (F3)", keymap != null))
            keymap!.OpenPalette();
        ImGui.SameLine(0, 4);
        if (ToolIcons.IconButton("##shortcuts", ToolIcon.Keyboard, ButtonSize, false,
                "Keyboard shortcuts (F1)", keymap != null))
            keymap!.OpenCheatSheet();
        ImGui.SameLine(0, 4);
        if (ImGui.Button("?", new Vector2(ButtonSize, ButtonSize)))
            tutorial.Open();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Quick tour of the spawn editor");

        ImGui.EndGroup();

        var stripMin = ImGui.GetItemRectMin() - new Vector2(6, 6);
        var stripMax = ImGui.GetItemRectMax() + new Vector2(6, 6);
        dl.ChannelsSetCurrent(0);
        WDE.MapRenderer.Utils.ImGuiIconButtons.Backdrop(dl, stripMin, stripMax);
        dl.ChannelsMerge();

        // clicks on the strip must not fall through into the world underneath
        engine.GameView.BlockClicksOver(new RectangleF(stripMin.X, stripMin.Y, stripMax.X - stripMin.X, stripMax.Y - stripMin.Y));

        ImGui.End();

        tutorial.RenderGUI();
    }

    // tooltip strings rebuilt only when the underlying state changes - the toolbar draws per frame
    private string cachedUndoTooltip = "", cachedRedoTooltip = "";
    private (bool avail, bool canUndo, bool canRedo, string? undoOp, string? redoOp, bool selectTool) undoRedoKey =
        (false, false, false, "-", "-", false);

    private (string undo, string redo) UndoRedoTooltips()
    {
        var key = (editService.IsAvailable, editService.CanUndo, editService.CanRedo,
            editService.CanUndo ? editService.NextUndo : null,
            editService.CanRedo ? editService.NextRedo : null,
            toolService.ActiveTool == SpawnEditorTool.Select);
        if (key != undoRedoKey)
        {
            undoRedoKey = key;
            // outside the Select tool the scope is easy to mistake - spell it out
            string scope = key.Item6 ? "" : " spawn edit";
            const string unavailable = "spawn editing is not connected in this host";
            cachedUndoTooltip = key.Item2 ? $"Undo{scope}: {key.Item4} (Ctrl+Z)"
                : key.Item1 ? "Undo - no spawn edits to undo" : $"Undo - {unavailable}";
            cachedRedoTooltip = key.Item3 ? $"Redo{scope}: {key.Item5} (Ctrl+Shift+Z)"
                : key.Item1 ? "Redo - no spawn edits to redo" : $"Redo - {unavailable}";
        }
        return (cachedUndoTooltip, cachedRedoTooltip);
    }

    // The gizmo-mode dropdown button: always shows the CURRENT mode's icon (Move/Rotate/Scale) plus
    // a caret; clicking opens a menu with the three modes. Not a tool - an orthogonal shared state
    // (pressing R while a spawn is selected is the same as picking Rotate here). Modes the active
    // tool doesn't support are greyed out.
    private void GizmoModeDropdown(ImDrawListPtr dl)
    {
        var (icon, label) = toolService.GizmoMode switch
        {
            GizmoMode.Rotate => (ToolIcon.Rotate, "Rotate"),
            GizmoMode.Scale => (ToolIcon.Scale, "Scale"),
            _ => (ToolIcon.Translate, "Move"),
        };

        bool anySupported = toolService.ActiveTool.SupportsAnyGizmo();
        string tooltip = label switch
        {
            "Rotate" => "Gizmo: Rotate (G grab, R rotate)",
            "Scale" => "Gizmo: Scale (G grab, R rotate)",
            _ => "Gizmo: Move (G grab, R rotate)",
        };
        if (ToolIcons.IconButton("##gizmomode", icon, ButtonSize, false,
                anySupported ? tooltip : "The active tool has no transform gizmo", anySupported))
            ImGui.OpenPopup("##gizmo_mode_menu");

        // dropdown caret in the button's corner
        var max = ImGui.GetItemRectMax();
        WDE.MapRenderer.Utils.ImGuiIconButtons.DropdownCaret(dl, max,
            ImGui.GetColorU32(anySupported ? ImGuiCol.Text : ImGuiCol.TextDisabled, 0.8f));

        // anchor the menu under the button, dropdown-style
        ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, max.Y + 4));
        if (ImGuiEx.BeginPopup("##gizmo_mode_menu"))
        {
            GizmoModeRow(ToolIcon.Translate, "Move", "G", GizmoMode.Translate);
            GizmoModeRow(ToolIcon.Rotate, "Rotate", "R", GizmoMode.Rotate);
            GizmoModeRow(ToolIcon.Scale, "Scale", null, GizmoMode.Scale);
            ImGui.Separator();
            bool showHandles = toolService.ShowGizmoHandles;
            if (ImGui.MenuItem("Show gizmo handles", "", ref showHandles))
                toolService.ShowGizmoHandles = showHandles;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hide the on-screen arrows/rings if you edit with G/R -\nthe keyboard grabs keep working");
            ImGui.EndPopup();
        }
    }

    private void GizmoModeRow(ToolIcon icon, string label, string? shortcut, GizmoMode mode)
    {
        bool supported = toolService.ActiveTool.Supports(mode);
        ImGui.BeginDisabled(!supported);
        var pos = ImGui.GetCursorScreenPos();
        float iconSize = ImGui.GetTextLineHeight();
        // leading spaces reserve room for the glyph drawn over them
        if (ImGui.Selectable($"      {label}{(shortcut != null ? $"  ({shortcut})" : "")}", toolService.GizmoMode == mode))
            toolService.GizmoMode = mode;
        uint col = ImGui.GetColorU32(supported ? ImGuiCol.Text : ImGuiCol.TextDisabled);
        ToolIcons.DrawIcon(ImGui.GetWindowDrawList(), icon, pos + new Vector2(2, 1), iconSize - 2, col);
        ImGui.EndDisabled();
        if (!supported && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(mode == GizmoMode.Scale
                ? "Spawns have no scale column in the database - there would be nothing to save"
                : "The active tool doesn't support this mode");
    }

    private void VerticalSeparator(ImDrawListPtr dl)
    {
        ImGui.SameLine(0, 8);
        var pos = ImGui.GetCursorScreenPos();
        dl.AddLine(new Vector2(pos.X, pos.Y + 4), new Vector2(pos.X, pos.Y + ButtonSize - 4), ImGui.GetColorU32(ImGuiCol.Border), 1f);
        ImGui.SameLine(0, 8 + 1);
    }

    // ---- ISavable: the 3D document's modified flag + Save command land here -----------------------

    public ReactiveProperty<bool> IsModified { get; } = new(false);

    // the document's Save (UI thread) parks a completion here; the game loop starts the actual save
    private readonly System.Collections.Concurrent.ConcurrentQueue<TaskCompletionSource> externalSaveRequests = new();

    public Task Save()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        externalSaveRequests.Enqueue(tcs);
        return tcs.Task;
    }

    // the document's "Generate query" (UI thread) parks a completion here, served on the game loop
    private readonly System.Collections.Concurrent.ConcurrentQueue<TaskCompletionSource<string?>> generateQueryRequests = new();

    public Task<string?> GenerateQuery()
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        generateQueryRequests.Enqueue(tcs);
        return tcs.Task;
    }

    /// <summary>Game thread, every frame: refreshes the aggregated dirty flag and serves queued
    /// document-Save requests (the editors' state must only be touched on the game loop).</summary>
    public void Update()
    {
        IsModified.Value = AnythingToSave();

        while (generateQueryRequests.TryDequeue(out var queryTcs))
        {
            var tcs = queryTcs;
            BuildPendingSaveSql().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception!.InnerExceptions);
                else
                    tcs.TrySetResult(t.Result);
            });
        }

        if (externalSaveRequests.IsEmpty)
            return;
        RequestSaveAll();
        var running = saveTask ?? Task.CompletedTask;
        while (externalSaveRequests.TryDequeue(out var tcs))
        {
            running.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception!.InnerExceptions);
                else
                    tcs.TrySetResult();
            });
        }
    }

    /// <summary>The exact combined SQL SaveAll would execute right now, section per editor, in the
    /// same order SaveAll runs them. Pure - built on the game loop, nothing executes.</summary>
    private async Task<string?> BuildPendingSaveSql()
    {
        var sb = new System.Text.StringBuilder();

        void Section(string title, WDE.SqlQueryGenerator.IQuery? query)
        {
            if (query == null || string.IsNullOrWhiteSpace(query.QueryString))
                return;
            sb.AppendLine($"-- {title}");
            sb.AppendLine(query.QueryString.Trim());
        }

        // spawns first, mirroring SaveAll (the bridge builds theirs from the hosted documents)
        var spawnsSql = await editService.GenerateSaveQuery();
        if (!string.IsNullOrWhiteSpace(spawnsSql))
        {
            sb.AppendLine("-- Spawns");
            sb.AppendLine(spawnsSql!.Trim());
        }

        if (formationService.AnyDirty)
            Section("Formations", formationService.BuildSaveQuery());
        if (spawnGroupService.AnyDirty)
            Section("Spawn groups", spawnGroupService.BuildSaveQuery());
        if (poolService.AnyDirty)
            Section("Pools", poolService.BuildSaveQuery());
        foreach (var path in waypointService.LoadedPaths)
        {
            if (path.IsDirty)
                Section($"Waypoints (path {path.Key})", waypointService.BuildSaveQuery(path));
        }
        if (safeLocService.AnyDirty)
            Section("Graveyards", safeLocService.BuildSaveQuery());
        if (spellTargetService.AnyDirty)
            Section("Spell targets", spellTargetService.BuildSaveQuery());
        if (linkService.AnyDirty)
            Section("Creature linking", linkService.BuildSaveQuery());
        if (areaTriggerService.AnyDirty)
            Section("Area triggers", areaTriggerService.BuildSaveQuery());

        return sb.Length == 0 ? null : sb.ToString();
    }

    private bool AnythingToSave() =>
        (editService.IsAvailable && editService.HasChanges)
        || formationService.AnyDirty
        || spawnGroupService.AnyDirty
        || poolService.AnyDirty
        || waypointService.AnyDirty
        || safeLocService.AnyDirty
        || spellTargetService.AnyDirty
        || linkService.AnyDirty
        || areaTriggerService.AnyDirty;

    /// <summary>Starts a SaveAll unless one is already running - the document save and the Ctrl+S
    /// shortcut both go through here so they share the busy guard.</summary>
    public void RequestSaveAll()
    {
        if (saveTask is { IsCompleted: false })
            return;
        saveTask = SaveAll();
        saveTask.ListenErrors(); // backstop only - SaveAll reports failures itself
    }

    private async Task SaveAll()
    {
        if (editService.IsAvailable && editService.HasChanges)
            editService.Save(); // event-driven; the bridge answers on the UI thread and reports its own outcome

        // one failing editor must not silently swallow the rest: save each independently,
        // collect failures and report them where the user is looking - in the 3D view
        List<string>? failures = null;
        int attempted = 0;

        async Task TrySave(string editor, Func<Task> save)
        {
            attempted++;
            try
            {
                await save();
            }
            catch (Exception e)
            {
                LOG.LogError(e, "Failed to save {Editor}", editor);
                (failures ??= new()).Add($"{editor}: {e.Message}");
            }
        }

        if (formationService.AnyDirty)
            await TrySave("Formations", formationService.Save);
        if (spawnGroupService.AnyDirty)
            await TrySave("Spawn groups", spawnGroupService.Save);
        if (poolService.AnyDirty)
            await TrySave("Pools", poolService.Save);
        if (waypointService.AnyDirty)
            await TrySave("Waypoints", waypointService.SaveAllDirty);
        if (safeLocService.AnyDirty)
            await TrySave("Graveyards", safeLocService.Save);
        if (spellTargetService.AnyDirty)
            await TrySave("Spell targets", spellTargetService.Save);
        if (linkService.AnyDirty)
            await TrySave("Creature linking", linkService.Save);
        if (areaTriggerService.AnyDirty)
            await TrySave("Area triggers", areaTriggerService.Save);

        if (failures != null)
        {
            var message = "Save failed:\n" + string.Join("\n", failures);
            if (attempted > failures.Count)
                message += $"\n({attempted - failures.Count} other editor{(attempted - failures.Count == 1 ? "" : "s")} saved fine)";
            notifications.Notify(GameNotificationType.Error, message);
        }
        else if (attempted > 0)
            notifications.Notify(GameNotificationType.Success, "Saved");
    }

    private void ToolButton(string id, ToolIcon icon, SpawnEditorTool tool, bool supported, string tooltip, int hotkey)
    {
        bool active = toolService.ActiveTool == tool;
        if (!tooltipCache.TryGetValue(tool, out var tips))
        {
            var idle = supported ? tooltip + $"\nHotkey: {hotkey}" : tooltip;
            tips = (idle, idle + "\n(active - click again or press Esc to return to Select)");
            tooltipCache[tool] = tips;
        }
        tooltip = active ? tips.active : tips.idle;
        if (ToolIcons.IconButton(id, icon, ButtonSize, active, tooltip, supported, ImGui.GetColorU32(ActiveColor)))
            toolService.ActiveTool = active ? SpawnEditorTool.Select : tool;
    }

    // (tool hotkey dispatch moved to SpawnEditorKeymap - the single source of truth the F1
    // cheat sheet and the F3 palette render from)
}
