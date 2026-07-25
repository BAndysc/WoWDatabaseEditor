using TheEngine;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Hexa.NET.ImGui;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Pools;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// Dear ImGui, all-maps spawns tree. Renders entirely on the render thread (no Avalonia).
/// Hierarchy: Map -> Zone -> [Area] -> [SpawnGroup] -> Entry -> Spawn.
/// Virtualized with ImGuiListClipper. Filtering auto-expands matches (phantom); a manual
/// expand/collapse click sticks across filter changes. Double-clicking a spawn moves the camera.
/// </summary>
public class SpawnsTreeWindow
{
    private readonly IGameContext gameContext;
    private readonly IDatabaseProvider databaseProvider;
    private readonly ISpawnSelectionService selectionService;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly ISpawnGroupEditorService groupEditor;
    private readonly IPoolEditorService poolEditor;
    private readonly IWorldSpawnEditService editService;
    private readonly IGamePhaseService gamePhaseService;
    private readonly IMainThread mainThread;
    private readonly IClipboardService clipboardService;
    private readonly SpawnsTreeSettings treeSettings;

    public SpawnsTreeWindow(IGameContext gameContext,
        IDatabaseProvider databaseProvider,
        ISpawnSelectionService selectionService,
        ISpawnsContainer spawnsContainer,
        ISpawnGroupEditorService groupEditor,
        IPoolEditorService poolEditor,
        IWorldSpawnEditService editService,
        IGamePhaseService gamePhaseService,
        IMainThread mainThread,
        IClipboardService clipboardService,
        SpawnsTreeSettings treeSettings)
    {
        this.gamePhaseService = gamePhaseService;
        this.gameContext = gameContext;
        this.databaseProvider = databaseProvider;
        this.selectionService = selectionService;
        this.spawnsContainer = spawnsContainer;
        this.groupEditor = groupEditor;
        this.poolEditor = poolEditor;
        this.editService = editService;
        this.mainThread = mainThread;
        this.clipboardService = clipboardService;
        this.treeSettings = treeSettings;
        // IUserSettings is main-thread only; the render loop picks the results up when they land
        mainThread.Dispatch(() => persistedExpandToApply = treeSettings.LoadExpandState());
        mainThread.Dispatch(() => pendingOnlyLoadedMap = treeSettings.LoadOnlyLoadedMap() ? 1 : 0);
    }

    // SpawnViewer attaches ITS SpawnContextMenu instance (its reload queue is the one SpawnViewer
    // polls) so tree right-clicks offer the exact same actions as right-clicking the model in 3D
    private SpawnContextMenu? contextMenu;
    public void AttachContextMenu(SpawnContextMenu menu) => contextMenu = menu;

    private volatile List<SpawnTreeNode>? builtRoots;
    private volatile List<SpawnRow>? cachedRows; // expensive DB load, reused for cheap regroups
    private int builtForGroupRevision = -1;
    private int builtForPoolRevision = -1;
    private List<SpawnTreeNode>? roots;
    private bool building;
    private string? buildError;
    private bool autoStarted; // the first window open kicks the build off itself
    // written by the background load, read by the UI frame (stage is volatile, counts are advisory)
    private volatile string? buildStage;
    private int buildDone, buildTotal;

    private string search = "";
    private string appliedSearch = "";
    private bool searching;
    private int matchCount;
    private float searchDebounceLeft;
    private const float SearchDebounceSeconds = 0.18f;

    private readonly List<(SpawnTreeNode node, int depth)> flat = new();
    private bool flatDirty;
    private SpawnTreeNode? selected;

    // "only the loaded map" filter: hide every map root except the one loaded in the 3D view. Persisted
    // via SpawnsTreeSettings; the pending value (-1 = not loaded yet, 0/1 = false/true) is set on the
    // main thread by the settings load and adopted in the render loop. lastFilterLoadedMap re-flattens
    // when the loaded map changes while the filter is on.
    private bool onlyLoadedMap;
    private volatile int pendingOnlyLoadedMap = -1;
    private int? lastFilterLoadedMap;
    private readonly ClampNote offMapNote = new(); // "that spawn is on another map" click feedback

    // sync with the 3D selection (ISpawnSelectionService)
    private readonly Dictionary<(bool isCreature, uint guid), SpawnTreeNode> guidLookup = new();
    private SpawnInstance? lastExternalSelection;
    private SpawnTreeNode? pendingScrollNode;

    // guid -> live SpawnInstance for the currently loaded map (tree click -> 3D selection)
    private int? instanceLookupMap;
    private readonly Dictionary<(bool isCreature, uint guid), SpawnInstance> instanceLookup = new();

    public void RenderGUI()
    {
        // must run even while the window is hidden - it snapshots the pending-spawn list every
        // frame so a Save that happens with the tree collapsed is still spliced in
        SyncCommittedEdits();

        // engine-side actions queued by the native context menu (UI thread)
        while (engineActions.TryDequeue(out var action))
            action();

        // a fresh right press invalidates whatever the previous one prepared; the row handlers
        // below overwrite this when the press landed on a row
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            SetContextRequest(false, null);

        if (!ImGui.Begin("Spawns"))
        {
            ImGui.End();
            return;
        }

        // right press over the tree window (rows or empty space): the native menu is ours -
        // the world/spawn menu must not appear underneath the tree
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
            SetContextRequest(true, null);

        ApplyPersistedExpand();

        // adopt a tree that finished building on the background task
        var incoming = builtRoots;
        if (incoming != null && !ReferenceEquals(incoming, roots))
        {
            var previousSelection = selected;
            roots = incoming;
            selected = null;
            ClearSelectionReveal(); // the reveal path pointed at the now-discarded nodes
            RestoreExpand(roots); // a rebuild makes fresh nodes - carry the user's expand state over
            BuildGuidLookup();
            // carry the selection over too: re-resolve the selected spawn among the fresh nodes
            // and keep it scrolled into view (nodes may have moved to another group/pool)
            if (previousSelection is { IsLeaf: true } &&
                guidLookup.TryGetValue((previousSelection.Kind == SpawnNodeKind.CreatureSpawn, previousSelection.Guid), out var reselected))
                RevealNode(reselected);
            ApplyFilter();
            flatDirty = true;
        }

        SyncFromExternalSelection();

        // reflect unsaved spawn-group/pool edits: rebuild the grouping (cheap, no DB) when an editor changed
        if (!building && cachedRows != null &&
            (groupEditor.StructureRevision != builtForGroupRevision || poolEditor.StructureRevision != builtForPoolRevision))
            StartRegroup();

        DrawToolbar();

        // first open builds the tree by itself; after an error the user retries via Reload
        if (roots == null && !building && buildError == null && !autoStarted)
        {
            autoStarted = true;
            StartBuild();
        }

        if (roots == null)
        {
            if (building)
            {
                // three-dot spinner + the load's current stage ("resolving areas  45,000/120,000")
                int dots = (int)(ImGui.GetTime() * 3) % 4;
                ImGui.TextUnformatted($"Loading spawns{new string('.', dots)}");
                if (buildStage is { } stage)
                {
                    int total = buildTotal;
                    ImGui.TextDisabled(total > 0 ? $"{stage}  {buildDone:N0}/{total:N0}" : stage);
                    if (total > 0)
                        ImGui.ProgressBar((float)buildDone / total, new Vector2(-1, 0));
                }
            }
            else if (buildError != null)
                ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), buildError);
            else
                ImGui.TextUnformatted("Press Load to build the spawns tree.");
            ImGui.End();
            return;
        }

        // an error with a tree still on screen (failed reload/regroup) must be visible too
        if (buildError != null)
        {
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - 30);
            ImGui.TextColored(new Vector4(1, 0.4f, 0.4f, 1), buildError);
            ImGui.PopTextWrapPos();
            ImGui.SameLine();
            if (ImGui.SmallButton("x##builderr"))
                buildError = null;
        }

        ImGui.SetNextItemWidth(search.Length > 0 ? -28f : -1f);
        if (ImGui.InputTextWithHint("##treesearch", "name / entry / #guid / zone or map (id)", ref search, 256))
            searchDebounceLeft = SearchDebounceSeconds;
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Words match independently against names, entries, #guid\nand zone/area/map names or ids");
        if (search.Length > 0)
        {
            ImGui.SameLine();
            if (ImGui.Button("x##clearsearch", new Vector2(22, 0)))
                search = ""; // the empty filter applies instantly below
        }

        if (!string.Equals(search, appliedSearch, StringComparison.Ordinal))
        {
            // the filter is a full recursive walk + reflatten of the all-maps tree - too heavy to
            // run per keystroke on big DBs, so apply it after a short typing pause (clearing is
            // instant, an empty filter costs nothing)
            searchDebounceLeft -= ImGui.GetIO().DeltaTime;
            if (searchDebounceLeft <= 0f || search.Length == 0)
            {
                appliedSearch = search;
                ApplyFilter();
                flatDirty = true;
            }
        }

        // adopt the persisted "only loaded map" value once the main-thread load lands
        if (pendingOnlyLoadedMap >= 0)
        {
            onlyLoadedMap = pendingOnlyLoadedMap == 1;
            pendingOnlyLoadedMap = -1;
            flatDirty = true;
        }
        // re-flatten when the loaded map changes while the filter is on (a different map becomes visible)
        if (onlyLoadedMap && lastFilterLoadedMap != spawnsContainer.LoadedMap)
            flatDirty = true;
        lastFilterLoadedMap = spawnsContainer.LoadedMap;

        if (flatDirty)
        {
            RebuildFlat();
            flatDirty = false;
        }

        offMapNote.Draw();
        DrawTree();

        // debounced expansion-state persistence (a click burst becomes one settings write)
        if (expandChanged)
        {
            expandChanged = false;
            expandSaveDelay = 2f;
        }
        if (expandSaveDelay > 0f)
        {
            expandSaveDelay -= ImGui.GetIO().DeltaTime;
            if (expandSaveDelay <= 0f)
                PersistExpand();
        }

        ImGui.End();
    }

    private void DrawToolbar()
    {
        if (ImGui.Button(building ? "Loading..." : (roots == null ? "Load" : "Reload")) && !building)
            StartBuild();
        if (roots != null)
        {
            ImGui.SameLine();
            if (ImGui.Button("+##expandall"))
            {
                foreach (var r in roots)
                    SetExpandRecursive(r, ExpandState.Expanded);
                collapsedWhileFiltering.Clear();
                collapsedWhileRevealing.Clear();
                flatDirty = true;
                expandChanged = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Expand all");
            ImGui.SameLine(0, 2);
            if (ImGui.Button("-##collapseall"))
            {
                foreach (var r in roots)
                {
                    SetExpandRecursive(r, ExpandState.Collapsed);
                    if (searching) // honored over the filter's phantom expand, like arrow clicks
                        CollectContainers(r, collapsedWhileFiltering);
                    if (selectionRevealPath.Count > 0) // likewise honored over the selection-reveal phantom
                        CollectContainers(r, collapsedWhileRevealing);
                }
                flatDirty = true;
                expandChanged = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Collapse all (back to the map list)");
            ImGui.SameLine(0, 2);
            // index loop, not List.Find(lambda): this runs every frame and the closure over loadedMap
            // allocated a Predicate<SpawnTreeNode> + closure per frame
            SpawnTreeNode? loadedMapNode = null;
            if (spawnsContainer.LoadedMap is { } loadedMap && roots != null)
            {
                for (int i = 0; i < roots.Count; ++i)
                {
                    if (roots[i].Kind == SpawnNodeKind.Map && roots[i].Map == loadedMap)
                    {
                        loadedMapNode = roots[i];
                        break;
                    }
                }
            }
            ImGui.BeginDisabled(loadedMapNode == null);
            if (ImGui.Button("@##locatemap") && loadedMapNode != null)
                RevealNode(loadedMapNode);
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(loadedMapNode != null
                    ? "Scroll to the loaded map"
                    : "No map is loaded in the 3D view");

            ImGui.SameLine(0, 8);
            if (ImGui.Checkbox("This map only", ref onlyLoadedMap))
            {
                flatDirty = true;
                bool value = onlyLoadedMap;
                mainThread.Dispatch(() => treeSettings.SaveOnlyLoadedMap(value));
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hide every map except the one loaded in the 3D view");

            if (searching)
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"{matchCount} matches");
            }
        }

        DrawPhasesButton();
    }

    private string phasesButtonLabel = "Phases";
    private int phasesButtonCount = -1;

    /// <summary>The in-game phase picker - which phases the 3D view shows. Works for both phasing
    /// models: phase MASKS (bit checkboxes) and phase IDS (DBC list), whichever the core uses -
    /// the service filled <see cref="IGamePhaseService.Phases"/> accordingly.</summary>
    private void DrawPhasesButton()
    {
        var phases = gamePhaseService.Phases;
        if (phases.Count == 0)
            return;

        int active = gamePhaseService.ActivePhases.Count;
        if (active != phasesButtonCount)
        {
            phasesButtonCount = active;
            phasesButtonLabel = active > 0 ? $"Phases ({active})" : "Phases";
        }

        ImGui.SameLine();
        if (ImGui.Button(phasesButtonLabel))
            ImGui.OpenPopup("##phasespopup");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Which in-game phases the 3D view shows\n(spawns outside the active phases are hidden)");

        if (ImGuiEx.BeginPopup("##phasespopup"))
        {
            for (int i = 0; i < phases.Count; ++i)
            {
                var phase = phases[i];
                bool isActive = phase.Active;
                if (ImGui.Checkbox(phase.DisplayLabel, ref isActive))
                {
                    bool value = isActive;
                    var vm = phase;
                    // the phase VM and its Active observers live on the UI thread
                    mainThread.Dispatch(() => vm.Active = value);
                }
            }
            ImGui.EndPopup();
        }
    }

    private static void CollectContainers(SpawnTreeNode node, HashSet<SpawnTreeNode> into)
    {
        if (node.IsLeaf)
            return;
        into.Add(node);
        if (node.Children == null)
            return;
        foreach (var child in node.Children)
            CollectContainers(child, into);
    }

    private void DrawTree()
    {
        if (searching && flat.Count == 0)
        {
            ImGui.TextDisabled($"No spawns match \"{appliedSearch.Trim()}\"");
            return;
        }

        var arrowSize = ImGui.GetFrameHeight();
        var indentStep = arrowSize;
        var lineHeight = arrowSize; // arrow box is the tallest per-row item -> real row pitch
        var baseX = ImGui.GetCursorPosX();

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
        ImGui.BeginChild("items", ImGui.GetContentRegionAvail(), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        // scroll a freshly revealed node (from an external 3D selection) into view
        if (pendingScrollNode != null)
        {
            int idx = flat.FindIndex(t => ReferenceEquals(t.node, pendingScrollNode));
            if (idx >= 0)
            {
                // center the revealed row in the viewport (SetScrollY clamps the upper bound)
                float target = idx * lineHeight + lineHeight * 0.5f - ImGui.GetWindowHeight() * 0.5f;
                ImGui.SetScrollY(target < 0 ? 0 : target);
            }
            pendingScrollNode = null;
        }

        var rowBgWidth = ImGui.GetWindowWidth();
        var drawList = ImGui.GetWindowDrawList();
        var iconSize = ImGui.GetFontSize();

        var clipper = new ImGuiListClipper();
        clipper.Begin(flat.Count, lineHeight);
        while (clipper.Step())
        {
            for (int j = clipper.DisplayStart; j < clipper.DisplayEnd; j++)
            {
                if (j >= flat.Count)
                    break;

                var (node, depth) = flat[j];
                ImGui.PushID(j);

                // full-row hit target first, arrow + label overlaid on top
                bool isSelected = ReferenceEquals(node, selected);
                ImGui.SetCursorPosX(baseX);
                ImGui.SetNextItemAllowOverlap();
                if (ImGui.Selectable("##row", isSelected, ImGuiSelectableFlags.SpanAllColumns, new Vector2(rowBgWidth, lineHeight)))
                    Select(node);
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    Activate(node);
                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    Select(node); // select first, like a left click (also pushes the 3D selection)
                    // the native Avalonia context menu opens at the right-button release and asks
                    // for these items through SpawnViewer.GenerateContextMenu
                    SetContextRequest(true, BuildContextItems(node));
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.ForTooltip))
                    DrawRowTooltip(node);

                ImGui.SameLine();
                ImGui.SetCursorPosX(baseX + depth * indentStep);
                var arrowPos = ImGui.GetCursorScreenPos();

                if (node.HasChildren)
                {
                    bool open = EffectiveOpen(node);
                    ImGui.InvisibleButton("##exp", new Vector2(arrowSize, arrowSize));
                    var col = ImGui.GetColorU32(ImGui.IsItemHovered() ? ImGuiCol.Text : ImGuiCol.TextDisabled);
                    var glyphPos = arrowPos + new Vector2(0, (arrowSize - ImGui.GetFontSize()) * 0.5f);
                    ImGuiP.RenderArrow(drawList, glyphPos, col, open ? ImGuiDir.Down : ImGuiDir.Right);
                    if (ImGui.IsItemClicked())
                        UserToggleExpand(node);
                }
                else
                {
                    ImGui.Dummy(new Vector2(arrowSize, arrowSize));
                }

                // kind icon
                ImGui.SameLine(0, 2);
                var iconTopLeft = ImGui.GetCursorScreenPos();
                ImGui.Dummy(new Vector2(iconSize, arrowSize));
                DrawKindIcon(drawList, new Vector2(iconTopLeft.X + iconSize * 0.5f, iconTopLeft.Y + arrowSize * 0.5f), iconSize * 0.5f, node.Kind);

                ImGui.SameLine(0, 4);
                ImGui.AlignTextToFramePadding();
                if (node.IsLeaf && editService.IsPendingDelete(node.Kind == SpawnNodeKind.CreatureSpawn, node.Guid))
                    ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 0.7f), node.PendingDeleteLabel);
                else if (searching && node.MatchesFilter)
                    // matched rows stand out from the ancestor/descendant context rows around them
                    ImGui.TextColored(new Vector4(1f, 0.93f, 0.55f, 1f), node.Label);
                else if (node.Kind == SpawnNodeKind.Map && node.Map == spawnsContainer.LoadedMap)
                    ImGui.TextColored(new Vector4(0.55f, 0.82f, 1f, 1f), node.LoadedLabel);
                else if (!node.IsLeaf && node.PoolId != 0)
                    // entry-wide pooled "(all spawns)" display row - not navigable, so don't look clickable
                    ImGui.TextDisabled(node.Label);
                else
                    ImGui.TextUnformatted(node.Label);
                if (node.IsLeaf && node.PoolId != 0)
                {
                    // pooled, but shown elsewhere (under its group, or pooled entry-wide)
                    ImGui.SameLine(0, 6);
                    ImGui.TextColored(new Vector4(0.70f, 0.55f, 0.95f, 0.9f), node.PoolBadge);
                }
                ImGui.PopID();
            }
        }
        clipper.End();

        // keyboard parity with double-click: Enter activates the highlighted row (fly-to / toggle)
        if (selected != null && ImGui.IsWindowFocused() && !ImGui.GetIO().WantTextInput &&
            ImGui.IsKeyPressed(ImGuiKey.Enter))
            Activate(selected);

        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    // --- committed-edit splicing (keeps the tree in sync after Save without a full DB reload) ------

    private int lastSpliceSaveCounter = -1;
    private IReadOnlyList<PendingSpawn> prevNewSpawns = Array.Empty<PendingSpawn>();
    private readonly List<PendingSpawn> spliceAdds = new();
    private readonly List<(bool isCreature, uint guid)> spliceDeletes = new();

    private void SyncCommittedEdits()
    {
        if (lastSpliceSaveCounter == -1)
            lastSpliceSaveCounter = editService.SaveCounter; // don't replay a save from before we existed

        if (editService.SaveCounter != lastSpliceSaveCounter)
        {
            lastSpliceSaveCounter = editService.SaveCounter;
            if (cachedRows != null)
            {
                // prevNewSpawns is last frame's pending list = the spawns this save just committed
                spliceAdds.AddRange(prevNewSpawns);
                spliceDeletes.AddRange(editService.LastSaveDeleted);
            }
        }
        prevNewSpawns = editService.NewSpawns;

        // apply once no background build is reading cachedRows (mutating it mid-build would race)
        if (!building && cachedRows is { } rows && (spliceAdds.Count > 0 || spliceDeletes.Count > 0))
        {
            bool changed = false;
            if (spliceDeletes.Count > 0)
            {
                var deleted = spliceDeletes.ToHashSet();
                changed |= rows.RemoveAll(r => deleted.Contains((r.IsCreature, r.Guid))) > 0;
            }
            foreach (var s in spliceAdds)
            {
                if (rows.Any(r => r.IsCreature == s.IsCreature && r.Guid == s.Guid))
                    continue;
                var (zone, area) = SpawnTreeBuilder.ResolveZoneArea(gameContext.DbcManager,
                    gameContext.ZoneAreaManager, s.Map, s.Position);
                // the spawn was just placed on the loaded map, so its live instance has the template
                string name = FindLiveInstance(s.IsCreature, s.Guid, s.Map, refreshOnMiss: true) switch
                {
                    CreatureSpawnInstance c => c.CreatureTemplate.Name,
                    GameObjectSpawnInstance g => g.GameObjectTemplate.Name,
                    _ => "Unknown",
                };
                rows.Add(new SpawnRow(s.IsCreature, s.Entry, s.Guid, s.Map, s.Position, zone, area, name));
                changed = true;
            }
            spliceAdds.Clear();
            spliceDeletes.Clear();
            if (changed)
                StartRegroup(force: true); // rebuild the visible tree from the updated rows
        }
    }

    // --- right-click context menu (served through the native Avalonia menu) ------------------------
    // A right press over the tree records a request here (render thread); the Avalonia layer opens
    // its menu at the release and SpawnViewer.GenerateContextMenu consumes the request (UI thread).

    private readonly object contextRequestLock = new();
    private bool contextOverTree;
    private List<(string, ICommand, object?)>? contextItems;
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> engineActions = new();

    private void SetContextRequest(bool overTree, List<(string, ICommand, object?)>? items)
    {
        lock (contextRequestLock)
        {
            contextOverTree = overTree;
            contextItems = items;
        }
    }

    /// <summary>UI thread. True when the last right press landed on the tree window - the caller
    /// must then show <paramref name="items"/> (null = no menu at all) instead of the world menu.</summary>
    public bool TryConsumeContextMenuRequest(out List<(string, ICommand, object?)>? items)
    {
        lock (contextRequestLock)
        {
            items = contextItems;
            bool over = contextOverTree;
            contextItems = null;
            contextOverTree = false;
            return over;
        }
    }

    // commands run on the UI thread; tree/engine mutations hop back to the render loop
    private ICommand GameCommand(Action action) => new Prism.Commands.DelegateCommand(() => engineActions.Enqueue(action));
    private ICommand UiCommand(Action action) => new Prism.Commands.DelegateCommand(action);

    private List<(string, ICommand, object?)> BuildContextItems(SpawnTreeNode node)
        => node.IsLeaf ? BuildLeafContextItems(node) : BuildContainerContextItems(node);

    private List<(string, ICommand, object?)> BuildLeafContextItems(SpawnTreeNode node)
    {
        var items = new List<(string, ICommand, object?)>();
        bool onLoadedMap = node.Map == spawnsContainer.LoadedMap;
        items.Add((onLoadedMap ? "Fly camera here" : "Fly camera here (loads the map)",
            GameCommand(() => gameContext.SetMap(node.Map, node.Position)), null));

        var inst = FindLiveInstance(node.Kind == SpawnNodeKind.CreatureSpawn, node.Guid, node.Map);
        if (inst != null && contextMenu != null)
        {
            // the right-click Select() pushed this instance into the shared selection, so the 3D
            // context menu generator yields exactly the items a world right-click would (edit row,
            // copy guid/position/orientation, update values, waypoints)
            items.Add(("-", AlwaysDisabledCommand.Command, null));
            if (contextMenu.GenerateContextMenu() is { } spawnItems)
                items.AddRange(spawnItems);
            items.Add(("-", AlwaysDisabledCommand.Command, null));
            items.Add(($"Copy entry  ({node.Entry})", UiCommand(() => clipboardService.SetText(node.Entry.ToString())), null));
        }
        else
        {
            // spawn not live on the loaded map - offer the row-data copies instead
            items.Add(("-", AlwaysDisabledCommand.Command, null));
            items.Add(($"Copy guid  ({node.Guid})", UiCommand(() => clipboardService.SetText(node.Guid.ToString())), null));
            items.Add(($"Copy entry  ({node.Entry})", UiCommand(() => clipboardService.SetText(node.Entry.ToString())), null));
            items.Add(("Copy position", UiCommand(() => clipboardService.SetText($"X: {node.Position.X} Y: {node.Position.Y} Z: {node.Position.Z}")), null));
            if (!onLoadedMap)
            {
                items.Add(("-", AlwaysDisabledCommand.Command, null));
                items.Add(("Edit row, waypoints... - load the spawn's map first", AlwaysDisabledCommand.Command, null));
            }
        }
        return items;
    }

    private List<(string, ICommand, object?)> BuildContainerContextItems(SpawnTreeNode node)
    {
        var items = new List<(string, ICommand, object?)>
        {
            ("Expand all inside", GameCommand(() =>
            {
                SetExpandRecursive(node, ExpandState.Expanded);
                flatDirty = true;
                expandChanged = true;
            }), null),
            ("Collapse all inside", GameCommand(() =>
            {
                SetExpandRecursive(node, ExpandState.Collapsed);
                flatDirty = true;
                expandChanged = true;
            }), null),
        };

        if (node.Kind is SpawnNodeKind.CreatureEntry or SpawnNodeKind.GameObjectEntry)
        {
            items.Add(("-", AlwaysDisabledCommand.Command, null));
            items.Add(($"Copy entry  ({node.Entry})", UiCommand(() => clipboardService.SetText(node.Entry.ToString())), null));
        }
        else if (node.Kind is SpawnNodeKind.SpawnGroup or SpawnNodeKind.Pool)
        {
            items.Add(("-", AlwaysDisabledCommand.Command, null));
            items.Add(($"Copy {(node.Kind == SpawnNodeKind.SpawnGroup ? "group" : "pool")} id  ({node.Entry})",
                UiCommand(() => clipboardService.SetText(node.Entry.ToString())), null));
        }
        return items;
    }

    private static void SetExpandRecursive(SpawnTreeNode node, ExpandState state)
    {
        node.Expand = state;
        if (node.Children == null)
            return;
        foreach (var child in node.Children)
        {
            if (child.HasChildren)
                SetExpandRecursive(child, state);
        }
    }

    private void CopyToClipboard(string text) =>
        mainThread.Dispatch(() => clipboardService.SetText(text));

    // delayed hover tooltip: names the icon's kind, leaf coords + where the node lives
    // single-slot tooltip cache: only one row is hovered at a time, so caching the built strings on
    // every SpawnTreeNode bloated ~440K nodes with 5 mostly-null fields. Keep just the last hovered
    // node's strings here - rebuilt only when the hovered node changes, not every frame.
    private SpawnTreeNode? tooltipNode;
    private string? tooltipTitle;
    private string? tooltipDetail;
    private string? tooltipPool;
    private string? tooltipAncestry;

    private void DrawRowTooltip(SpawnTreeNode node)
    {
        if (!ReferenceEquals(tooltipNode, node))
        {
            tooltipNode = node;
            tooltipTitle = node.IsLeaf ? $"{KindName(node.Kind)}  #{node.Guid}" : KindName(node.Kind);
            tooltipDetail = node.IsLeaf
                ? $"Entry {node.Entry}  ·  {node.Position.X:0.0} / {node.Position.Y:0.0} / {node.Position.Z:0.0}"
                : null;
            tooltipPool = node.IsLeaf && node.PoolId != 0 ? $"Member of pool {node.PoolId}" : null;
            var path = AncestorPath(node);
            tooltipAncestry = path.Length > 0 ? "In: " + path : null;
        }

        ImGui.BeginTooltip();
        ImGui.TextUnformatted(tooltipTitle!);
        if (node.IsLeaf)
        {
            ImGui.TextDisabled(tooltipDetail!);
            if (tooltipPool != null)
                ImGui.TextColored(new Vector4(0.70f, 0.55f, 0.95f, 0.9f), tooltipPool);
        }
        else if (node.PoolId != 0)
            ImGui.TextDisabled("Display-only: the pool rolls every spawn of this entry,\nso there is no single position to navigate to");
        if (tooltipAncestry != null)
            ImGui.TextDisabled(tooltipAncestry);
        if (node.IsLeaf)
            ImGui.TextDisabled(node.Map == spawnsContainer.LoadedMap
                ? "Double-click: fly the camera there"
                : "Double-click: load the map and fly there");
        ImGui.EndTooltip();
    }

    private static string KindName(SpawnNodeKind kind) => kind switch
    {
        SpawnNodeKind.Map => "Map",
        SpawnNodeKind.Zone => "Zone",
        SpawnNodeKind.Area => "Area",
        SpawnNodeKind.SpawnGroup => "Spawn group",
        SpawnNodeKind.Pool => "Spawn pool",
        SpawnNodeKind.CreatureEntry => "Creature entry",
        SpawnNodeKind.GameObjectEntry => "Gameobject entry",
        SpawnNodeKind.CreatureSpawn => "Creature spawn",
        SpawnNodeKind.GameObjectSpawn => "Gameobject spawn",
        _ => kind.ToString(),
    };

    // "Eastern Kingdoms > Elwynn Forest > Goldshire > 123 Kobold" (count suffixes stripped)
    private static string AncestorPath(SpawnTreeNode node)
    {
        var parts = new List<string>();
        for (var p = node.Parent; p != null; p = p.Parent)
            parts.Add(StripCountSuffix(p.Label));
        parts.Reverse();
        return string.Join(" > ", parts);
    }

    private static string StripCountSuffix(string label)
    {
        int i = label.LastIndexOf("  (", StringComparison.Ordinal);
        return i > 0 ? label[..i] : label;
    }

    // per-kind vector icons drawn from primitives (no icon font is loaded); colors stay
    // color-coded per kind like the original shapes, the silhouettes carry the meaning
    private static void DrawKindIcon(ImDrawListPtr dl, Vector2 c, float r, SpawnNodeKind kind)
    {
        switch (kind)
        {
            case SpawnNodeKind.Map:
                DrawFoldedMap(dl, c, r);
                break;
            case SpawnNodeKind.Zone:
                DrawMountains(dl, c, r);
                break;
            case SpawnNodeKind.Area:
                DrawPin(dl, c, r, Col(0.35f, 0.68f, 0.72f));
                break;
            case SpawnNodeKind.SpawnGroup: // linked trio = a group
                DrawCluster(dl, c, r, Col(0.95f, 0.62f, 0.22f));
                break;
            case SpawnNodeKind.Pool: // a die = random selection
                DrawDie(dl, c, r, Col(0.70f, 0.52f, 0.95f));
                break;
            case SpawnNodeKind.CreatureEntry:
                DrawPerson(dl, c, r, Col(0.88f, 0.35f, 0.35f));
                break;
            case SpawnNodeKind.GameObjectEntry:
                DrawCog(dl, c, r, Col(0.85f, 0.72f, 0.30f));
                break;
            case SpawnNodeKind.CreatureSpawn: // one concrete placement: smaller, lighter
                DrawPerson(dl, c, r * 0.8f, Col(0.85f, 0.45f, 0.45f));
                break;
            case SpawnNodeKind.GameObjectSpawn:
                DrawCrate(dl, c, r * 0.75f, Col(0.80f, 0.70f, 0.40f));
                break;
        }
    }

    private static void DrawFoldedMap(ImDrawListPtr dl, Vector2 c, float r)
    {
        float w = r * 1.15f, h = r * 0.85f, fold = r * 0.35f;
        uint light = Col(0.60f, 0.65f, 0.80f);
        uint dark = Col(0.45f, 0.50f, 0.65f);
        float x1 = c.X - w, x2 = c.X - w / 3, x3 = c.X + w / 3, x4 = c.X + w;
        dl.AddQuadFilled(new Vector2(x1, c.Y - h + fold), new Vector2(x2, c.Y - h),
            new Vector2(x2, c.Y + h - fold), new Vector2(x1, c.Y + h), light);
        dl.AddQuadFilled(new Vector2(x2, c.Y - h), new Vector2(x3, c.Y - h + fold),
            new Vector2(x3, c.Y + h), new Vector2(x2, c.Y + h - fold), dark);
        dl.AddQuadFilled(new Vector2(x3, c.Y - h + fold), new Vector2(x4, c.Y - h),
            new Vector2(x4, c.Y + h - fold), new Vector2(x3, c.Y + h), light);
    }

    private static void DrawMountains(ImDrawListPtr dl, Vector2 c, float r)
    {
        float baseY = c.Y + r * 0.7f;
        dl.AddTriangleFilled(new Vector2(c.X - r, baseY), new Vector2(c.X - r * 0.15f, c.Y - r * 0.8f),
            new Vector2(c.X + r * 0.55f, baseY), Col(0.28f, 0.62f, 0.33f));
        dl.AddTriangleFilled(new Vector2(c.X - r * 0.05f, baseY), new Vector2(c.X + r * 0.5f, c.Y - r * 0.1f),
            new Vector2(c.X + r, baseY), Col(0.42f, 0.80f, 0.47f));
    }

    private static void DrawPin(ImDrawListPtr dl, Vector2 c, float r, uint col)
    {
        var head = new Vector2(c.X, c.Y - r * 0.3f);
        dl.AddCircleFilled(head, r * 0.62f, col);
        dl.AddTriangleFilled(new Vector2(c.X - r * 0.48f, c.Y + r * 0.02f),
            new Vector2(c.X + r * 0.48f, c.Y + r * 0.02f), new Vector2(c.X, c.Y + r * 0.95f), col);
        dl.AddCircleFilled(head, r * 0.24f, ImGui.GetColorU32(ImGuiCol.WindowBg));
    }

    private static void DrawCluster(ImDrawListPtr dl, Vector2 c, float r, uint col)
    {
        var top = c + new Vector2(0, -r * 0.65f);
        var left = c + new Vector2(-r * 0.7f, r * 0.55f);
        var right = c + new Vector2(r * 0.7f, r * 0.55f);
        dl.AddLine(top, left, col, 1.2f);
        dl.AddLine(left, right, col, 1.2f);
        dl.AddLine(right, top, col, 1.2f);
        dl.AddCircleFilled(top, r * 0.36f, col);
        dl.AddCircleFilled(left, r * 0.36f, col);
        dl.AddCircleFilled(right, r * 0.36f, col);
    }

    private static void DrawDie(ImDrawListPtr dl, Vector2 c, float r, uint col)
    {
        dl.AddRectFilled(c - new Vector2(r * 0.9f, r * 0.9f), c + new Vector2(r * 0.9f, r * 0.9f), col, r * 0.35f);
        uint pip = ImGui.GetColorU32(ImGuiCol.WindowBg);
        dl.AddCircleFilled(c + new Vector2(-r * 0.4f, -r * 0.4f), r * 0.17f, pip);
        dl.AddCircleFilled(c, r * 0.17f, pip);
        dl.AddCircleFilled(c + new Vector2(r * 0.4f, r * 0.4f), r * 0.17f, pip);
    }

    private static void DrawPerson(ImDrawListPtr dl, Vector2 c, float r, uint col)
    {
        dl.AddCircleFilled(new Vector2(c.X, c.Y - r * 0.45f), r * 0.42f, col);
        // shoulders: the top half-disc of a circle sitting below the head
        dl.PathArcTo(new Vector2(c.X, c.Y + r * 0.85f), r * 0.75f, MathF.PI, MathF.Tau);
        dl.PathFillConvex(col);
    }

    private static void DrawCog(ImDrawListPtr dl, Vector2 c, float r, uint col)
    {
        const int teeth = 6;
        for (int i = 0; i < teeth; ++i)
        {
            float angle = MathF.Tau * i / teeth;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var perp = new Vector2(-dir.Y, dir.X) * (r * 0.2f);
            var inner = c + dir * (r * 0.45f);
            var outer = c + dir * r;
            dl.AddQuadFilled(inner - perp, inner + perp, outer + perp, outer - perp, col);
        }
        dl.AddCircle(c, r * 0.52f, col, 0, r * 0.34f); // thick ring = body with a center hole
    }

    private static void DrawCrate(ImDrawListPtr dl, Vector2 c, float r, uint col)
    {
        var min = c - new Vector2(r, r);
        var max = c + new Vector2(r, r);
        uint dark = Col(0.45f, 0.38f, 0.20f);
        dl.AddRectFilled(min, max, col, 1f);
        dl.AddRect(min, max, dark);
        dl.AddLine(min, max, dark);
        dl.AddLine(new Vector2(min.X, max.Y), new Vector2(max.X, min.Y), dark);
    }

    private static uint Col(float r, float g, float b, float a = 1f) => ImGui.GetColorU32(new Vector4(r, g, b, a));

    private void Select(SpawnTreeNode node)
    {
        selected = node;
        // re-anchor the phantom reveal on the clicked node: drops any stale auto-expansion from a
        // previous 3D selection, and keeps this node's own ancestors open until it's deselected
        // (ancestors the user expanded explicitly stay open regardless - their Expand is Expanded).
        SetSelectionRevealPath(node);
        if (!node.IsLeaf)
            return;

        // push the tree selection into the shared 3D selection, if the spawn's live
        // instance exists on the currently loaded map. refreshOnMiss: a spawn placed
        // in-session on the loaded map is newer than the cached lookup snapshot.
        var inst = FindLiveInstance(node.Kind == SpawnNodeKind.CreatureSpawn, node.Guid, node.Map, refreshOnMiss: true);
        if (inst != null)
        {
            lastExternalSelection = inst; // our own change - don't treat it as external below
            selectionService.SelectedSpawn.Value = inst;
        }
        else if (node.Map != spawnsContainer.LoadedMap)
        {
            var top = node;
            while (top.Parent != null)
                top = top.Parent;
            offMapNote.Set($"#{node.Guid} is on {StripCountSuffix(top.Label)}, not the loaded map - double-click to load it and fly there");
        }
    }

    private void Activate(SpawnTreeNode node)
    {
        selected = node;
        if (node.IsLeaf)
        {
            gameContext.SetMap(node.Map, node.Position);
        }
        else
        {
            // double-clicking a container toggles it (sticky)
            UserToggleExpand(node);
        }
    }

    // --- selection sync ---

    private void SyncFromExternalSelection()
    {
        var ext = selectionService.SelectedSpawn.Value;
        if (ReferenceEquals(ext, lastExternalSelection))
            return;
        lastExternalSelection = ext;

        if (ext == null)
        {
            selected = null;
            ClearSelectionReveal(); // collapse the auto-expanded ancestors back
            return;
        }

        if (guidLookup.TryGetValue((ext is CreatureSpawnInstance, ext.Guid), out var node))
            RevealNode(node);
        else
        {
            // the selected spawn isn't in the current tree (e.g. another map) - drop the prior reveal
            selected = null;
            ClearSelectionReveal();
        }
    }

    private void RevealNode(SpawnTreeNode node)
    {
        selected = node;
        SetSelectionRevealPath(node);
        pendingScrollNode = node; // reveal from OUTSIDE the tree also scrolls the node into view
    }

    // phantom-expands the ancestor chain of the selected node rather than stickily marking it
    // Expanded: EffectiveOpen treats reveal-path nodes as open while the selection stands, and they
    // collapse back on deselect (see ClearSelectionReveal). No expandChanged - a reveal must never
    // persist as the user's expand state.
    private void SetSelectionRevealPath(SpawnTreeNode node)
    {
        selectionRevealPath.Clear();
        collapsedWhileRevealing.Clear();
        for (var p = node.Parent; p != null; p = p.Parent)
            selectionRevealPath.Add(p);
        flatDirty = true;
    }

    private void BuildGuidLookup()
    {
        guidLookup.Clear();
        if (roots == null)
            return;
        foreach (var r in roots)
            CollectLeaves(r);
    }

    private void CollectLeaves(SpawnTreeNode n)
    {
        if (n.IsLeaf)
        {
            guidLookup[(n.Kind == SpawnNodeKind.CreatureSpawn, n.Guid)] = n;
            return;
        }
        if (n.Children != null)
            foreach (var c in n.Children)
                CollectLeaves(c);
    }

    private SpawnInstance? FindLiveInstance(bool isCreature, uint guid, int map, bool refreshOnMiss = false)
    {
        if (spawnsContainer.LoadedMap != map)
            return null;

        if (instanceLookupMap != spawnsContainer.LoadedMap)
        {
            RebuildInstanceLookup();
        }

        if (instanceLookup.TryGetValue((isCreature, guid), out var inst))
            return inst;

        // the lookup snapshots the map's instances - a spawn placed after it was built misses
        if (refreshOnMiss)
        {
            RebuildInstanceLookup();
            if (instanceLookup.TryGetValue((isCreature, guid), out inst))
                return inst;
        }
        return null;
    }

    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();

    private void RebuildInstanceLookup()
    {
        instanceLookup.Clear();
        instanceLookupMap = spawnsContainer.LoadedMap;
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var s in spawnsScratch)
            instanceLookup[(s is CreatureSpawnInstance, s.Guid)] = s;
    }

    // collapses clicked during the CURRENT search - honored; collapses that predate the search are
    // overridden below (a collapsed Elwynn must not silently swallow the "Hogger" hit). Cleared on
    // every filter change - a new query is a new context.
    private readonly HashSet<SpawnTreeNode> collapsedWhileFiltering = new();

    // ancestors force-opened to reveal the externally-selected spawn (3D click -> tree). Phantom, like
    // the search expand: they revert to their prior state the moment the selection clears, so a spawn
    // selection doesn't permanently sprawl the tree open. A node the user expanded explicitly stays
    // open regardless (its Expand is Expanded, not touched here).
    private readonly HashSet<SpawnTreeNode> selectionRevealPath = new();
    // collapses the user clicked on a reveal-path ancestor DURING the current selection - honored over
    // the reveal's phantom open (mirrors collapsedWhileFiltering). Cleared whenever the reveal resets.
    private readonly HashSet<SpawnTreeNode> collapsedWhileRevealing = new();

    private bool EffectiveOpen(SpawnTreeNode n)
    {
        // the selection-reveal phantom forces the path to the selected spawn open, but a user collapse
        // during the selection (collapsedWhileRevealing) wins, exactly like the search phantom
        if (selectionRevealPath.Contains(n) && !collapsedWhileRevealing.Contains(n))
            return true;
        return n.Expand switch
        {
            ExpandState.Expanded => true,
            ExpandState.Collapsed => searching && n.HasVisibleDescendant && !collapsedWhileFiltering.Contains(n),
            _ => searching && n.HasVisibleDescendant, // phantom expand while filtering
        };
    }

    // Records a user expand/collapse click. Keeps the search- and selection-phantom overrides honest
    // so the click visibly takes effect even on a node that is only phantom-open (otherwise clicking
    // "collapse" on a phantom-expanded ancestor would appear to do nothing).
    private void UserToggleExpand(SpawnTreeNode node)
    {
        bool open = EffectiveOpen(node);
        node.Expand = open ? ExpandState.Collapsed : ExpandState.Expanded;
        if (searching)
        {
            if (open) collapsedWhileFiltering.Add(node);
            else collapsedWhileFiltering.Remove(node);
        }
        if (selectionRevealPath.Contains(node))
        {
            if (open) collapsedWhileRevealing.Add(node);
            else collapsedWhileRevealing.Remove(node);
        }
        flatDirty = true;
        expandChanged = true;
    }

    // Drops the selection-reveal phantom so the auto-expanded ancestors collapse back to how the user
    // left them. No-op when nothing was revealed, so the per-frame deselect path stays cheap.
    private void ClearSelectionReveal()
    {
        if (selectionRevealPath.Count == 0 && collapsedWhileRevealing.Count == 0)
            return;
        selectionRevealPath.Clear();
        collapsedWhileRevealing.Clear();
        flatDirty = true;
    }

    // --- expand-state preservation across rebuilds (new nodes are made each build) ---
    // and across restarts (seeded from SpawnsTreeSettings, saved back debounced)

    private readonly Dictionary<string, ExpandState> savedExpand = new();
    private volatile Dictionary<string, int>? persistedExpandToApply; // set on the main thread, consumed by the render loop
    private bool expandChanged;
    private float expandSaveDelay;

    private void ApplyPersistedExpand()
    {
        if (persistedExpandToApply is not { } persisted)
            return;
        persistedExpandToApply = null;
        foreach (var (path, state) in persisted)
            savedExpand.TryAdd(path, (ExpandState)state); // this session's own clicks win
        if (roots != null)
        {
            RestoreExpand(roots);
            flatDirty = true;
        }
    }

    private void PersistExpand()
    {
        CaptureExpand();
        var copy = new Dictionary<string, int>(savedExpand.Count);
        foreach (var (path, state) in savedExpand)
            copy[path] = (int)state;
        mainThread.Dispatch(() => treeSettings.SaveExpandState(copy));
    }

    // stable per-node token (independent of the label, which carries a changing count suffix)
    private static string Token(SpawnTreeNode n) => $"{(int)n.Kind}:{n.Map}:{n.Entry}";

    private void CaptureExpand()
    {
        if (roots == null)
            return; // before the first build: keep the persisted seed instead of wiping it
        savedExpand.Clear();
        foreach (var r in roots)
            CaptureExpand(r, "");
    }

    private void CaptureExpand(SpawnTreeNode n, string prefix)
    {
        if (n.IsLeaf)
            return;
        string path = prefix + Token(n);
        if (n.Expand != ExpandState.Default)
            savedExpand[path] = n.Expand;
        if (n.Children != null)
            foreach (var c in n.Children)
                CaptureExpand(c, path + "/");
    }

    private void RestoreExpand(List<SpawnTreeNode> newRoots)
    {
        if (savedExpand.Count == 0)
            return;
        foreach (var r in newRoots)
            RestoreExpand(r, "");
    }

    private void RestoreExpand(SpawnTreeNode n, string prefix)
    {
        if (n.IsLeaf)
            return;
        string path = prefix + Token(n);
        if (savedExpand.TryGetValue(path, out var st))
            n.Expand = st;
        if (n.Children != null)
            foreach (var c in n.Children)
                RestoreExpand(c, path + "/");
    }

    // full (re)load: pull all spawns from the DB, cache them, then build the tree.
    private void StartBuild()
    {
        if (building)
            return;
        building = true;
        buildError = null;
        CaptureExpand();
        int groupRevision = groupEditor.StructureRevision;
        int poolRevision = poolEditor.StructureRevision;
        var (overlay, names, useDbGroups) = SnapshotGroupOverlay();
        var (pools, useDbPools) = SnapshotPoolOverlay();
        FullBuildAsync(groupRevision, poolRevision, overlay, names, useDbGroups, pools, useDbPools).ListenErrors();
    }

    private async Task FullBuildAsync(int groupRevision, int poolRevision,
        Dictionary<(bool, uint), uint> overlay, Dictionary<uint, string> names, bool useDbGroups,
        PoolOverlay pools, bool useDbPools)
    {
        try
        {
            var dbc = gameContext.DbcManager;
            // awaits here resume ON THE GAME LOOP (TheEngineSynchronizationContext), not the thread
            // pool - so the per-spawn area-resolve loop yields a frame every chunk (it can't hop to
            // the thread pool: ZoneAreaManager may still be generating its area tables concurrently)
            var rows = await SpawnTreeBuilder.LoadRowsAsync(databaseProvider, dbc, gameContext.ZoneAreaManager,
                (stage, done, total) => { buildDone = done; buildTotal = total; buildStage = stage; },
                async () => await gameContext.Engine.NextFrame);
            cachedRows = rows;
            buildStage = "loading groups and pools";
            buildTotal = 0;
            if (useDbGroups)
                (overlay, names) = await SpawnTreeBuilder.LoadDbGroupOverlayAsync(databaseProvider);
            if (useDbPools)
                pools = await SpawnTreeBuilder.LoadDbPoolOverlayAsync(databaseProvider);
            buildStage = "building the tree";
            // pure CPU over local snapshots + immutable DBC stores - keep it off the game loop
            // (same as RegroupAsync) or it freezes the frame for seconds on big databases
            builtRoots = await Task.Run(() => SpawnTreeBuilder.BuildTree(rows, dbc, overlay, names, pools));
            builtForGroupRevision = groupRevision;
            builtForPoolRevision = poolRevision;
            buildError = null; // a stale error must not outlive a successful build
        }
        catch (Exception e)
        {
            buildError = e.Message;
        }
        finally
        {
            buildStage = null;
            buildTotal = 0;
            building = false;
        }
    }

    // cheap rebuild from cached rows + fresh group/pool overlays, for unsaved-edit refresh
    // (DB is only hit for an overlay whose editor hasn't loaded yet).
    private void StartRegroup(bool force = false)
    {
        var rows = cachedRows;
        if (building || rows == null || (!force && !groupEditor.HasData && !poolEditor.HasData))
        {
            // nothing to do; record BOTH revisions even on the early-out - or this re-arms every frame
            builtForGroupRevision = groupEditor.StructureRevision;
            builtForPoolRevision = poolEditor.StructureRevision;
            return;
        }
        building = true;
        CaptureExpand();
        int groupRevision = groupEditor.StructureRevision;
        int poolRevision = poolEditor.StructureRevision;
        var (overlay, names, useDbGroups) = SnapshotGroupOverlay();
        var (pools, useDbPools) = SnapshotPoolOverlay();
        RegroupAsync(rows, groupRevision, poolRevision, overlay, names, useDbGroups, pools, useDbPools).ListenErrors();
    }

    private async Task RegroupAsync(List<SpawnRow> rows, int groupRevision, int poolRevision,
        Dictionary<(bool, uint), uint> overlay, Dictionary<uint, string> names, bool useDbGroups,
        PoolOverlay pools, bool useDbPools)
    {
        try
        {
            var dbc = gameContext.DbcManager;
            if (useDbGroups)
                (overlay, names) = await SpawnTreeBuilder.LoadDbGroupOverlayAsync(databaseProvider);
            if (useDbPools)
                pools = await SpawnTreeBuilder.LoadDbPoolOverlayAsync(databaseProvider);
            builtRoots = await Task.Run(() => SpawnTreeBuilder.BuildTree(rows, dbc, overlay, names, pools));
            buildError = null; // a stale error must not outlive a successful regroup
        }
        catch (Exception e)
        {
            buildError = $"Refreshing the group/pool overlay failed: {e.Message}";
        }
        finally
        {
            // record the attempted revisions even on failure, or the failed regroup re-arms every
            // frame - an endless silent DB-hitting retry loop; the next EDIT (new revision) retries
            builtForGroupRevision = groupRevision;
            builtForPoolRevision = poolRevision;
            building = false;
        }
    }

    // snapshot the editors' in-memory grouping/pooling (DB + unsaved edits) on the engine thread,
    // so the background build reads an immutable copy. useDb=true when that editor hasn't loaded
    // yet - the flags are independent (one editor can be loaded while the other isn't).
    private (Dictionary<(bool, uint), uint> overlay, Dictionary<uint, string> names, bool useDb) SnapshotGroupOverlay()
    {
        var overlay = new Dictionary<(bool, uint), uint>();
        var names = new Dictionary<uint, string>();
        if (!groupEditor.HasData)
            return (overlay, names, true);

        var scratch = new List<SpawnGroupMember>();
        foreach (var (id, name) in groupEditor.GroupNames)
        {
            names[id] = name;
            groupEditor.CollectMembers(id, scratch);
            foreach (var m in scratch)
                overlay[(m.IsCreature, m.Guid)] = id;
        }
        return (overlay, names, false);
    }

    private (PoolOverlay pools, bool useDb) SnapshotPoolOverlay()
    {
        var pools = PoolOverlay.Empty;
        if (!poolEditor.HasData)
            return (pools, true);

        var scratch = new List<PoolMember>();
        foreach (var (id, name) in poolEditor.PoolNames)
        {
            pools.PoolNames[id] = name;
            poolEditor.CollectMembers(id, scratch);
            foreach (var m in scratch)
                pools.MemberToPool[(m.IsCreature, m.Guid)] = id;
            if (poolEditor.MotherOf(id) is { } mother)
                pools.MotherOf[id] = mother;
            if (poolEditor.GetDetails(id) is { } details)
                foreach (var entry in details.EntryMembers.Keys)
                    pools.EntryToPool[(entry.IsCreature, entry.Entry)] = id;
        }
        return (pools, false);
    }

    // --- filtering ---

    private void ApplyFilter()
    {
        searching = !string.IsNullOrWhiteSpace(appliedSearch);
        matchCount = 0;
        collapsedWhileFiltering.Clear();
        // trimmed, whitespace-separated tokens matched independently ("hogger elwynn" works;
        // a trailing space no longer kills every match)
        var tokens = appliedSearch.Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (roots == null)
            return;
        foreach (var r in roots)
            ComputeFilter(r, tokens);
    }

    private bool ComputeFilter(SpawnTreeNode n, string[] tokens)
    {
        bool matches = searching;
        if (matches)
        {
            foreach (var token in tokens)
            {
                // leaves don't store SearchText (it'd duplicate Label on ~367K nodes) - match Label directly
                bool hit = n.IsLeaf
                    ? n.Label.Contains(token, StringComparison.OrdinalIgnoreCase)
                    : n.SearchText.Contains(token, StringComparison.Ordinal);
                if (!hit)
                {
                    matches = false;
                    break;
                }
            }
        }
        n.MatchesFilter = matches;
        if (matches)
            matchCount++;
        bool anyChild = false;
        if (n.Children != null)
        {
            foreach (var c in n.Children)
                anyChild |= ComputeFilter(c, tokens);
        }
        n.HasVisibleDescendant = anyChild;
        return n.MatchesFilter || anyChild;
    }

    // --- flattening (respects filter + expand state) ---

    private void RebuildFlat()
    {
        flat.Clear();
        if (roots == null)
            return;
        // "only loaded map": drop every other map root (roots are all maps). Applies only while a map
        // is actually loaded - otherwise the filter would hide the whole tree, so fall back to showing
        // all. Search still works, scoped to the visible map(s).
        int? onlyMap = onlyLoadedMap ? spawnsContainer.LoadedMap : null;
        foreach (var r in roots)
        {
            if (onlyMap is { } m && r.Map != m)
                continue;
            Flatten(r, 0, false);
        }
    }

    private void Flatten(SpawnTreeNode n, int depth, bool ancestorMatched)
    {
        if (!IsShown(n, ancestorMatched))
            return;
        flat.Add((n, depth));
        if (n.HasChildren && EffectiveOpen(n))
        {
            bool am = ancestorMatched || n.MatchesFilter;
            foreach (var c in n.Children!)
                Flatten(c, depth + 1, am);
        }
    }

    private bool IsShown(SpawnTreeNode n, bool ancestorMatched)
        => !searching || ancestorMatched || n.MatchesFilter || n.HasVisibleDescendant;
}
