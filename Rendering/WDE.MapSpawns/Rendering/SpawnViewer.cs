using System.Collections;
using System.Windows.Input;
using Avalonia.Animation;
using Hexa.NET.ImGui;
using Prism.Ioc;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheEngine.Utils;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.Utils;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;
using WDE.MpqReader.Structures;

namespace WDE.MapSpawns.Rendering;

public class SpawnViewer : IGameModule
{
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly IGameContext gameContext;
    private readonly IEntityManager entityManager;
    private readonly IRenderManager renderManager;
    private readonly IGameEventService gameEventService;
    private readonly IGamePhaseService gamePhaseService;
    private readonly IInputManager inputManager;
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IWorldInteractionService worldInteraction;
    private readonly IMainThread mainThread;
    private readonly MdxManager mdxManager;
    private readonly AnimationSystem animationSystem;
    private readonly Engine engine;
    private readonly SpawnsTreeWindow spawnsTreeWindow;
    private readonly SpawnGroupVisualizer spawnGroupVisualizer;
    private readonly GameViewToolbar toolbar;
    private readonly GameViewInspector inspector;
    private readonly SelectInspectorSection selectSection;
    private readonly IGameViewOverlayService overlays;
    private readonly ISpawnEditorToolService toolService;
    private readonly IWorldSpawnEditService editService;
    private readonly SpawnPickerWindow spawnPicker;
    private readonly SpawnPlacementController placement;
    private readonly GameViewNotifications notifications;
    private readonly IGameNotificationService notificationService;
    private SpawnPreviewRenderStage? spawnPreview;
    public object? ViewModel => null;

    private HighlightPostProcess postProcess;

    private List<System.IDisposable> disposables = new();

    private SpawnDragger spawnDragger;
    private readonly SpawnContextMenu spawnContextMenu;
    private readonly SpawnEditorKeymap keymap;

    // native "Add creature/gameobject..." items: eligibility is recomputed on the engine thread at
    // every right-button press (true = the press landed on empty ground in Select mode with nothing
    // selected); the Avalonia layer asks via GenerateContextMenu at right-button release
    private bool addMenuEligible;
    private readonly ICommand addCreatureCommand;
    private readonly ICommand addGameObjectCommand;

    private RenderLayer renderLayer;

    private Entity selectionDecal;
    private bool selectionDecalCreated;
    private ITexture? selectionDecalTexture;
    private const float SelectionDecalVerticalHalf = 5.0f; // Z half-extent (catches ground around the feet)
    private const float SelectionRadiusPadding = 1.15f;    // a little breathing room around the model footprint
    private const float SelectionRadiusFallback = 2.0f;    // used when the spawn has no world bounds yet
    private const float SelectionRadiusMin = 1.0f;
    private const float SelectionRadiusMax = 20.0f;
    private static readonly Vector4 SelectionDecalColor = new(0.15f, 0.7f, 1.0f, 0.9f); // aqua tint, .w = opacity

    // grouped by template entry, not by chunk, since a given entry's spawns span many chunks
    private Entity spawnsRoot;
    private Entity creaturesRoot;
    private Entity gameObjectsRoot;
    private Dictionary<uint, Entity> creatureEntryNodes = new();
    private Dictionary<uint, Entity> gameObjectEntryNodes = new();

    private Entity EnsureSpawnsRoot()
    {
        if (spawnsRoot == Entity.Empty)
            spawnsRoot = entityManager.CreateEntity(gameContext.Archetypes.GroupArchetype, "Spawns"u8);
        return spawnsRoot;
    }

    private Entity EnsureCreatureEntryNode(uint entry, string name)
    {
        if (!creatureEntryNodes.TryGetValue(entry, out var node))
        {
            if (creaturesRoot == Entity.Empty)
            {
                creaturesRoot = entityManager.CreateEntity(gameContext.Archetypes.GroupArchetype, "Creatures"u8);
                entityManager.SetParent(creaturesRoot, EnsureSpawnsRoot());
            }
            node = entityManager.CreateEntity(gameContext.Archetypes.GroupArchetype, $"{entry} {name}");
            entityManager.SetParent(node, creaturesRoot);
            creatureEntryNodes[entry] = node;
        }
        return node;
    }

    private Entity EnsureGameObjectEntryNode(uint entry, string name)
    {
        if (!gameObjectEntryNodes.TryGetValue(entry, out var node))
        {
            if (gameObjectsRoot == Entity.Empty)
            {
                gameObjectsRoot = entityManager.CreateEntity(gameContext.Archetypes.GroupArchetype, "GameObjects"u8);
                entityManager.SetParent(gameObjectsRoot, EnsureSpawnsRoot());
            }
            node = entityManager.CreateEntity(gameContext.Archetypes.GroupArchetype, $"{entry} {name}");
            entityManager.SetParent(node, gameObjectsRoot);
            gameObjectEntryNodes[entry] = node;
        }
        return node;
    }

    public SpawnViewer(ICachedDatabaseProvider databaseProvider,
        ISpawnsContainer spawnsContainer,
        IGameContext gameContext,
        IEntityManager entityManager,
        IRenderManager renderManager,
        IGameEventService gameEventService,
        IGamePhaseService gamePhaseService,
        IInputManager inputManager,
        ISpawnSelectionService spawnSelectionService,
        IWorldInteractionService worldInteraction,
        IMainThread mainThread,
        MdxManager mdxManager,
        AnimationSystem animationSystem,
        Engine engine,
        
        SpawnDragger spawnDragger,
        SpawnContextMenu spawnContextMenu,
        SpawnsTreeWindow spawnsTreeWindow,
        SpawnGroupVisualizer spawnGroupVisualizer,
        GameViewToolbar toolbar,
        GameViewInspector inspector,
        SelectInspectorSection selectSection,
        IGameViewOverlayService overlays,
        ISpawnEditorToolService toolService,
        IWorldSpawnEditService editService,
        SpawnPickerWindow spawnPicker,
        SpawnPlacementController placement,
        GameViewNotifications notifications,
        IGameNotificationService notificationService,
        IChangesManager changesManager,
        Models.Waypoints.IWaypointEditorService waypointService,
        SpawnEditorKeymap keymap)
    {
        this.keymap = keymap;
        this.notificationService = notificationService;
        // the toolbar aggregates every 3D editor's dirty state; registering it makes the hosting
        // document's IsModified (tab asterisk) and Save command work
        disposables.Add(changesManager.AddSavable(toolbar));
        this.waypointService = waypointService;
        this.databaseProvider = databaseProvider;
        this.spawnsContainer = spawnsContainer;
        this.gameContext = gameContext;
        this.entityManager = entityManager;
        this.renderManager = renderManager;
        this.gameEventService = gameEventService;
        this.gamePhaseService = gamePhaseService;
        this.inputManager = inputManager;
        this.spawnSelectionService = spawnSelectionService;
        this.worldInteraction = worldInteraction;
        this.mainThread = mainThread;
        this.mdxManager = mdxManager;
        this.animationSystem = animationSystem;
        this.engine = engine;
        this.spawnsTreeWindow = spawnsTreeWindow;
        this.spawnGroupVisualizer = spawnGroupVisualizer;
        this.toolbar = toolbar;
        this.inspector = inspector;
        this.selectSection = selectSection;
        this.overlays = overlays;
        this.toolService = toolService;
        this.editService = editService;
        this.spawnPicker = spawnPicker;
        this.placement = placement;
        this.notifications = notifications;
        this.spawnDragger = spawnDragger;
        this.spawnContextMenu = spawnContextMenu;
        postProcess = new HighlightPostProcess(gameContext.Engine, Color.Aqua);

        // executed on the UI thread by the native menu; OpenFor only flips picker flags
        addCreatureCommand = new Prism.Commands.DelegateCommand(() => spawnPicker.OpenFor(creatures: true));
        addGameObjectCommand = new Prism.Commands.DelegateCommand(() => spawnPicker.OpenFor(creatures: false));

        disposables.Add(spawnDragger);
        // phases/events are toggled on the UI thread; RefreshVisibility touches engine objects,
        // so hop the emissions onto the game loop
        disposables.Add(gameEventService.ActiveEventsObservable.ObserveOnGameLoop().Subscribe(_ => RefreshVisibility()));
        disposables.Add(gamePhaseService.ActivePhasesObservable.ObserveOnGameLoop().Subscribe(_ => RefreshVisibility()));
    }
    
    public void Initialize()
    {
        selectSection.AttachGroupVisualizer(spawnGroupVisualizer);
        selectSection.AttachDragger(spawnDragger); // numeric transform fields commit through it
        selectSection.AttachPlacement(placement); // hint bar shows the placement HUD
        selectSection.AttachContextMenu(spawnContextMenu); // "Edit row" button reuses the shared flow
        spawnsTreeWindow.AttachContextMenu(spawnContextMenu); // tree right-clicks reuse the 3D menu

        overlays.SetSection(SpawnEditorTool.Select, selectSection);
        renderLayer = gameContext.Engine.RenderManager.RegisterRenderLayer("Map Spawns");
        gameContext.Engine.RenderManager.AddPostprocess(postProcess);

        selectionDecalTexture = BuildSelectionCircleTexture();
        selectionDecal = entityManager.CreateEntity(entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<Decal>(), "Spawn selection decal");
        selectionDecalCreated = true;
        ref var decal = ref entityManager.GetComponent<Decal>(selectionDecal);
        decal.Disabled = true;
        decal.Color = SelectionDecalColor;
        decal.FadeAngleCos = 0.3f; // only land on ground-ish surfaces, not near-vertical walls
        decal.Albedo = selectionDecalTexture;

        spawnGroupVisualizer.Initialize();
        placement.Initialize(renderLayer);

        spawnPreview = new SpawnPreviewRenderStage(gameContext, databaseProvider, entityManager, renderLayer);
        gameContext.Engine.RenderManager.RegisterRenderStage(spawnPreview);
        spawnPicker.AttachPreview(spawnPreview);

        // SpawnViewer is the single game-scope owner of the picker + placement controller (neither is
        // [AutoRegister], so they build in the game container where IGameContext exists)
        spawnPicker.AttachPlacement(placement);

        // the keymap dispatches the global hotkeys and renders the F1 cheat sheet / F3 palette;
        // the toolbar draws its two buttons and serves its Save command
        keymap.Attach(toolbar, spawnPicker, placement);
        toolbar.AttachKeymap(keymap);

        // MMB orbit pivots on the selected spawn while it is actually in view; otherwise the camera
        // orbits whatever it looks at (raycast) - never yanked toward an off-screen selection
        gameContext.CameraManager.OrbitPivotProvider = OrbitPivot;
    }

    private Vector3? OrbitPivot()
    {
        if (spawnSelectionService.SelectedSpawn.Value is not { IsSpawned: true } spawn)
            return null;
        var pos = spawn.WorldObject?.Position ?? spawn.Position;
        var camera = engine.CameraManager.MainCamera;
        var toSpawn = pos - camera.Transform.Position;
        float dist = toSpawn.Length();
        if (dist < 0.5f || dist > 400f)
            return null;
        var forward = Vectors.Down.Multiply(camera.Transform.Rotation); // the camera convention (see CameraManager)
        return Vector3.Dot(toSpawn / dist, forward) > 0.5f ? pos : null;
    }
    
    public void Dispose()
    {
        gameContext.CameraManager.OrbitPivotProvider = null;
        overlays.SetSection(SpawnEditorTool.Select, null);
        gameContext.Engine.RenderManager.UnregisterRenderLayer(renderLayer);
        if (spawnPreview != null)
        {
            gameContext.Engine.RenderManager.UnregisterRenderStage(spawnPreview);
            spawnPreview.Dispose();
            spawnPreview = null;
        }
        placement.Dispose();
        spawnGroupVisualizer.Dispose();
        spawnSelectionService.SelectedSpawn.Value = null;
        spawnsContainer.Clear();

        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            if (spawn.IsSpawned)
                spawn.Dispose();
        }
        
        foreach (var d in disposables)
            d.Dispose();
        disposables.Clear();
        gameContext.Engine.RenderManager.RemovePostprocess(postProcess);
        postProcess.Dispose();
        pendingSpawnInstances.Clear();
        pendingSpawnLoads.Clear();

        // module can be re-created without the EntityManager session ending, so tear the tree down too
        if (spawnsRoot != Entity.Empty)
            entityManager.DestroyEntity(spawnsRoot);
        spawnsRoot = Entity.Empty;
        creaturesRoot = Entity.Empty;
        gameObjectsRoot = Entity.Empty;
        creatureEntryNodes.Clear();
        gameObjectEntryNodes.Clear();

        if (selectionDecalCreated && entityManager.Exist(selectionDecal))
            entityManager.DestroyEntity(selectionDecal);
        selectionDecalCreated = false;
        gameContext.Engine.TextureManager.DisposeTexture(selectionDecalTexture);
        selectionDecalTexture = null;
    }

    public void Update(float delta)
    {
        // before the tool-specific early-returns: save/undo/redo must work in every tool mode
        HandleGlobalShortcuts();

        toolbar.Update();

        // keep the UI-thread context-menu commands supplied with a safe transform snapshot
        spawnContextMenu.PublishSelectedSpawnTransform(spawnSelectionService.SelectedSpawn.Value);

        UpdateSpawnsData();

        UpdateSelectionDecal();

        spawnGroupVisualizer.Update(delta);

        spawnPreview?.TickAnimation(delta); // drive the picker preview model's idle (it's far from the world camera)

        // reflect pending edits when the bridge state changes
        if (editService.Revision != lastEditRevision)
        {
            lastEditRevision = editService.Revision;
            ProcessCommittedSave();
            SyncPendingSpawns();
            RefreshPendingEditVisuals();
        }

        // context-menu duplicate requests (UI thread) -> start the phantom placement here
        while (spawnContextMenu.TryDequeueDuplicate(out var spawnToDuplicate))
            placement.BeginDuplicate(spawnToDuplicate is CreatureSpawnInstance, spawnToDuplicate.Entry, spawnToDuplicate.Guid);

        // spawns whose 1:1 table editor was closed: reload them from the DB (row may have changed)
        while (spawnContextMenu.TryDequeueReload(out var spawnToReload))
            ReloadSpawn(spawnToReload).ListenErrors();

        // the spawn grabber/gizmo belongs to the Select tool only - other tools (waypoints,
        // formations, groups) own the pointer and draw their own manipulators
        if (toolService.ActiveTool == SpawnEditorTool.Select)
            spawnDragger.Update(delta);
        else
            spawnDragger.Deactivate();

        // phantom placement takes over the pointer while active
        placement.Update(delta);
        if (placement.IsPlacing)
        {
            hoveredSpawn = null;
            return;
        }

        // world click-to-select only in Select mode; the other tools handle their own clicks
        if (toolService.ActiveTool != SpawnEditorTool.Select)
        {
            hoveredSpawn = null;
            NotifySelectionLockedClick();
            // the mouse-only tools have no Delete of their own - a silently swallowed keypress
            // reads as a broken key, so say where deleting lives (other tools delete their own
            // selected point/link and never reach this branch)
            if (toolService.ActiveTool is SpawnEditorTool.SpawnGroup or SpawnEditorTool.Pool &&
                (inputManager.Keyboard.JustPressed(Key.Delete) || inputManager.Keyboard.JustPressed(Key.Back)) &&
                !worldInteraction.IsCaptured)
                notificationService.Notify(GameNotificationType.Info, "Deleting spawns happens in the Select tool (press 1)");
            return;
        }

        // Delete marks the selected spawn for deletion (pending until Save). Back(space) too: on macOS
        // the physical delete key reports as Backspace (forward-delete needs Fn). Not while a grab is
        // in flight - Backspace then edits the typed numeric entry.
        if (editService.IsAvailable && !worldInteraction.IsCaptured &&
            (inputManager.Keyboard.JustPressed(Key.Delete) || inputManager.Keyboard.JustPressed(Key.Back)) &&
            spawnSelectionService.SelectedSpawn.Value is { } toDelete)
        {
            editService.ToggleDelete(toDelete is CreatureSpawnInstance, toDelete.Entry, toDelete.Guid, (int)gameContext.CurrentMapId);
        }

        // "Add creature/gameobject" eligibility is per right-button press; the native menu opens at
        // the matching release (and only when the press wasn't a camera-rotate drag)
        if (inputManager.Mouse.HasJustClicked(MouseButton.Right))
            addMenuEligible = false;

        if (worldInteraction.PointerUsedThisFrame)
        {
            hoveredSpawn = null;
            return;
        }

        UpdateHover();

        if (inputManager.Mouse.HasJustClicked(MouseButton.Left) ||
            (spawnSelectionService.SelectedSpawn.Value == null && inputManager.Mouse.HasJustClicked(MouseButton.Right)))
        {
            bool rightClick = inputManager.Mouse.HasJustClicked(MouseButton.Right);
            var pickedEntity = renderManager.PickObject(inputManager.Mouse.NormalizedPosition);

            // right click that hits no spawn -> the native menu offers Add creature/gameobject;
            // a left click on empty space deselects
            if (pickedEntity.IsEmpty())
            {
                addMenuEligible = rightClick;
                if (!rightClick)
                    spawnSelectionService.SelectedSpawn.Value = null;
                return;
            }

            pickedEntity = pickedEntity.GetRoot(entityManager);

            if (!entityManager.Exist(pickedEntity))
                return;

            if (!entityManager.HasManagedComponent<SpawnInstance>(pickedEntity))
            {
                addMenuEligible = rightClick;
                if (!rightClick)
                    spawnSelectionService.SelectedSpawn.Value = null;
                return;
            }

            var spawn = spawnSelectionService.SelectedSpawn.Value = entityManager.GetManagedComponent<SpawnInstance>(pickedEntity);

            if (inputManager.Mouse.HasJustDoubleClicked)
            {
                // same flow as the context menu's "Edit creature/gameobject": save-confirm, open the
                // 1:1 editor, reload the spawn from the DB after it closes
                mainThread.Dispatch(() => spawnContextMenu.EditRowCommand.Execute(spawn));
            }
        }
    }

    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        // right press landed on the spawns tree window: it supplies the whole menu (null = the
        // press hit the tree's empty space - show nothing rather than the world menu underneath)
        if (spawnsTreeWindow.TryConsumeContextMenuRequest(out var treeItems))
            return treeItems;

        // the SpawnGroup tool has its own right-click menu (add/remove/leader) - don't stack
        // the generic spawn menu under it
        if (toolService.ActiveTool == SpawnEditorTool.SpawnGroup)
            return null;

        var spawnItems = spawnContextMenu.GenerateContextMenu();
        if (!addMenuEligible || toolService.ActiveTool != SpawnEditorTool.Select)
            return spawnItems;

        var items = spawnItems?.ToList() ?? new List<(string, ICommand, object?)>();
        if (items.Count > 0)
            items.Add(("-", AlwaysDisabledCommand.Command, null));
        items.Add(("Add creature...", addCreatureCommand, null));
        items.Add(("Add gameobject...", addGameObjectCommand, null));
        return items;
    }

    /// <summary>Replaces a spawn's instance with a fresh one built from the current DB row - used
    /// after its 1:1 table editor closes (the row may have changed or been deleted).</summary>
    private async Task ReloadSpawn(SpawnInstance spawn)
    {
        bool wasSelected = ReferenceEquals(spawnSelectionService.SelectedSpawn.Value, spawn);

        if (spawn is CreatureSpawnInstance)
        {
            var fresh = await databaseProvider.GetCreatureByGuidAsync(spawn.Entry, spawn.Guid);
            RemoveSpawnFromWorld(spawn);
            if (fresh == null)
                return; // row deleted in the editor - the spawn is gone

            var template = databaseProvider.GetCachedCreatureTemplate(fresh.Entry) ?? await databaseProvider.GetCreatureTemplate(fresh.Entry);
            if (template == null)
                return;

            var instance = new CreatureSpawnInstance(fresh, template)
            {
                Addon = await databaseProvider.GetCreatureAddon(fresh.Entry, fresh.Guid)
            };
            spawnsContainer.AddExternalSpawn(instance, template, null);
            await LoadSpawnAsync(instance, CancellationToken.None);
            if (wasSelected)
                spawnSelectionService.SelectedSpawn.Value = instance;
        }
        else if (spawn is GameObjectSpawnInstance)
        {
            var fresh = await databaseProvider.GetGameObjectByGuidAsync(spawn.Entry, spawn.Guid);
            RemoveSpawnFromWorld(spawn);
            if (fresh == null)
                return;

            var template = databaseProvider.GetCachedGameObjectTemplate(fresh.Entry) ?? await databaseProvider.GetGameObjectTemplate(fresh.Entry);
            if (template == null)
                return;

            var instance = new GameObjectSpawnInstance(fresh, template);
            spawnsContainer.AddExternalSpawn(instance, null, template);
            await LoadSpawnAsync(instance, CancellationToken.None);
            if (wasSelected)
                spawnSelectionService.SelectedSpawn.Value = instance;
        }
    }

    private readonly List<Entity?> highlightEntities = new(1) { null };
    public void Render(float delta)
    {
        // the selection highlight belongs to the Select tool (and the SpawnGroup tool, whose
        // clicks also target spawns); in the Waypoint tool it stays only while editing the
        // SELECTED creature's own path, elsewhere it is just noise
        SpawnInstance? highlighted = toolService.ActiveTool switch
        {
            SpawnEditorTool.Select or SpawnEditorTool.SpawnGroup => spawnSelectionService.SelectedSpawn.Value,
            SpawnEditorTool.Waypoint when spawnSelectionService.SelectedSpawn.Value is CreatureSpawnInstance selectedCreature &&
                                          waypointService.EditingPath is { AutoLoadedFromCreatureGuid: > 0 } path &&
                                          path.AutoLoadedFromCreatureGuid == selectedCreature.Guid
                => selectedCreature,
            _ => null,
        };
        highlightEntities[0] = highlighted?.WorldObject?.WorldObjectEntity;
        postProcess.Render(highlightEntities);
    }

    // hover feedback (Select tool): stall-free per-frame deferred GPU pick -> tooltip in RenderGUI
    private SpawnInstance? hoveredSpawn;

    /// <summary>Non-Select tools ignore clicks on spawns; a click on an unselected spawn there gets
    /// a small toast so the no-op is explained. The SpawnGroup tool is exempt - it selects itself.</summary>
    private void NotifySelectionLockedClick()
    {
        if (toolService.ActiveTool == SpawnEditorTool.SpawnGroup)
            return;
        if (!inputManager.Mouse.HasJustClicked(MouseButton.Left))
            return;
        if (worldInteraction.PointerUsedThisFrame || worldInteraction.IsCaptured || placement.IsPlacing)
            return;
        var picked = renderManager.PickObject(inputManager.Mouse.NormalizedPosition);
        if (picked.IsEmpty())
            return;
        picked = picked.GetRoot(entityManager);
        if (!entityManager.Exist(picked) || !entityManager.HasManagedComponent<SpawnInstance>(picked))
            return;
        if (entityManager.GetManagedComponent<SpawnInstance>(picked) is not { } spawn)
            return;
        if (ReferenceEquals(spawn, spawnSelectionService.SelectedSpawn.Value))
            return;
        notificationService.Notify(GameNotificationType.Info, "Switch to the Select tool to change the selection");
    }

    private void UpdateHover()
    {
        hoveredSpawn = null;
        if (worldInteraction.IsCaptured) // mid-drag/grab: the tooltip would just flicker in the way
            return;

        var picked = renderManager.PickObjectDeferred(inputManager.Mouse.NormalizedPosition);
        if (picked.IsEmpty())
            return;
        picked = picked.GetRoot(entityManager);
        if (!entityManager.Exist(picked) || !entityManager.HasManagedComponent<SpawnInstance>(picked))
            return;
        if (entityManager.GetManagedComponent<SpawnInstance>(picked) is { IsSpawned: true } spawn)
            hoveredSpawn = spawn;
    }

    // the phantom takes a moment to load after picking a template - without this the cursor sits
    // in a silent dead zone where clicks do nothing
    private void DrawPlacementLoadingMarker()
    {
        if (!placement.IsLoadingPhantom || !engine.GameView.IsHovered)
            return;
        int dots = (int)(ImGui.GetTime() * 3) % 4;
        var mouse = ImGui.GetMousePos();
        ImGui.GetForegroundDrawList().AddText(mouse + new System.Numerics.Vector2(18, 14),
            ImGui.GetColorU32(ImGuiCol.Text, 0.9f), $"loading the model{new string('.', dots)}");
    }

    private void DrawHoverTooltip()
    {
        if (hoveredSpawn is not { IsSpawned: true } spawn)
            return;
        // only when the pointer is really over the world - not over another window or an item of
        // the in-view chrome (toolbar buttons, inspector, toasts)
        if (!engine.GameView.IsHovered || ImGui.IsAnyItemHovered() || ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
            return;

        bool isCreature = spawn is CreatureSpawnInstance;
        string name = spawn switch
        {
            CreatureSpawnInstance c => c.CreatureTemplate.Name,
            GameObjectSpawnInstance g => g.GameObjectTemplate.Name,
            _ => "",
        };
        string text = $"{name}\n{(isCreature ? "creature" : "gameobject")} {spawn.Entry} · guid {spawn.Guid}";
        if (editService.IsPendingDelete(isCreature, spawn.Guid))
            text += "\nmarked for deletion";
        else if (editService.NewSpawns.Any(n => n.IsCreature == isCreature && n.Guid == spawn.Guid))
            text += "\nnot saved yet";
        ImGui.SetTooltip(text);
    }

    /// <summary>The global hotkeys live in <see cref="SpawnEditorKeymap"/> (the same table the F1
    /// cheat sheet and F3 palette render) - only the Escape step-out chain stays here. Typing in an
    /// ImGui field can't trigger any of it: the ImGui controller clears the engine key queue
    /// whenever ImGui wants the keyboard, and IsDown is focus-gated to the 3D view.</summary>
    private readonly Models.Waypoints.IWaypointEditorService waypointService;
    private bool escapeBlockedLastFrame;

    private void HandleGlobalShortcuts()
    {
        keymap.HandleHotkeys();
        HandleEscapeChain(inputManager.Keyboard);
    }

    /// <summary>Escape that no in-flight interaction consumed steps outward, innermost first: an
    /// open ImGui popup always wins (it closes itself, see <see cref="ImGuiEx.BeginPopup"/>), then
    /// the active tool's local state (waypoint pen mode / point selection, consumed in the tool
    /// module), then leaving the tool for Select - and only in the Select tool does Escape
    /// deselect the spawn, so a tool never loses its subject to a stray press. The one-frame
    /// history guard keeps it from double-firing with a consumer (popup close / drag cancel /
    /// placement cancel / pen-mode exit / keymap overlay close) that released its state in the
    /// same frame the key was pressed - module update order is not guaranteed.</summary>
    private void HandleEscapeChain(TheEngine.Interfaces.IKeyboard kb)
    {
        bool waypointToolOwnsEscape = toolService.ActiveTool == SpawnEditorTool.Waypoint &&
            (waypointService.EditingPath != null ||
             (waypointService.SelectedPath is { } path &&
              waypointService.SelectedPointIndex >= 0 && waypointService.SelectedPointIndex < path.Points.Count));

        bool blocked = worldInteraction.IsCaptured
            || placement.IsPlacing
            || keymap.AnyOverlayOpen
            || spawnPicker.ConsumesEscape
            || ImGuiEx.AnyPopupOpen
            || waypointToolOwnsEscape;

        if (kb.JustPressed(Key.Escape) && !blocked && !escapeBlockedLastFrame)
        {
            if (toolService.ActiveTool != SpawnEditorTool.Select)
                toolService.ActiveTool = SpawnEditorTool.Select; // leave the tool, keep the selection
            else if (spawnSelectionService.SelectedSpawn.Value != null)
                spawnSelectionService.SelectedSpawn.Value = null;
        }

        escapeBlockedLastFrame = blocked;
    }

    public void RenderGUI()
    {
        toolbar.RenderGUI();
        inspector.RenderGUI();
        spawnsTreeWindow.RenderGUI();
        spawnPicker.RenderGUI();
        spawnDragger.DrawGuides(); // axis-lock guide line while a grab is constrained
        DrawHoverTooltip();
        DrawPlacementLoadingMarker();
        keymap.RenderGUI(); // F1 cheat sheet + F3 command palette
        notifications.RenderGUI(); // last: toasts draw on top of the other in-view chrome
    }

    public void RenderTransparent()
    {
        spawnDragger.RenderTransparent();
    }

    private int lastEditRevision = -1;
    private int lastSaveCounter;
    // spawns created this session (pending until Save): live SpawnInstance + its mutable backing data
    private readonly Dictionary<(bool isCreature, uint guid), (SpawnInstance instance, object data)> pendingSpawnInstances = new();
    private readonly HashSet<(bool isCreature, uint guid)> pendingSpawnLoads = new();

    // after a Save: pending new spawns became real DB rows (keep their instances, stop tracking) and
    // pending deletes were committed (remove those spawns from the world + tree).
    private void ProcessCommittedSave()
    {
        if (editService.SaveCounter == lastSaveCounter)
            return;
        lastSaveCounter = editService.SaveCounter;
        pendingSpawnInstances.Clear();
        foreach (var (isCreature, guid) in editService.LastSaveDeleted)
        {
            if (FindSpawn(isCreature, guid) is { } spawn)
                RemoveSpawnFromWorld(spawn);
        }
    }

    // mirror editService.NewSpawns -> live world instances: placing spawns them immediately,
    // undo (or delete) of a pending spawn removes it again, moves follow the bridge state.
    private void SyncPendingSpawns()
    {
        var wanted = editService.NewSpawns;

        foreach (var key in pendingSpawnInstances.Keys.ToList())
        {
            if (!wanted.Any(s => s.IsCreature == key.isCreature && s.Guid == key.guid))
            {
                RemoveSpawnFromWorld(pendingSpawnInstances[key].instance);
                pendingSpawnInstances.Remove(key);
            }
        }

        foreach (var spawn in wanted)
        {
            var key = (spawn.IsCreature, spawn.Guid);
            if (pendingSpawnInstances.TryGetValue(key, out var live))
                ApplyPendingTransform(live, spawn);
            else if (!pendingSpawnLoads.Contains(key))
            {
                pendingSpawnLoads.Add(key);
                CreatePendingSpawn(spawn).ListenErrors();
            }
        }
    }

    private async Task CreatePendingSpawn(PendingSpawn s)
    {
        Console.WriteLine($"[SpawnViewer] creating live instance for pending {(s.IsCreature ? "creature" : "gameobject")} {s.Entry} (guid {s.Guid})");
        var key = (s.IsCreature, s.Guid);
        try
        {
            SpawnInstance instance;
            object data;
            if (s.IsCreature)
            {
                var template = databaseProvider.GetCachedCreatureTemplate(s.Entry) ?? await databaseProvider.GetCreatureTemplate(s.Entry);
                if (template == null)
                    return;
                var creatureData = new PendingCreatureData
                {
                    Guid = s.Guid, Entry = s.Entry, Map = s.Map,
                    X = s.Position.X, Y = s.Position.Y, Z = s.Position.Z, O = s.Orientation
                };
                instance = new CreatureSpawnInstance(creatureData, template);
                data = creatureData;
                spawnsContainer.AddExternalSpawn(instance, template, null);
            }
            else
            {
                var template = databaseProvider.GetCachedGameObjectTemplate(s.Entry) ?? await databaseProvider.GetGameObjectTemplate(s.Entry);
                if (template == null)
                    return;
                var goData = new PendingGameObjectData
                {
                    Guid = s.Guid, Entry = s.Entry, Map = s.Map,
                    X = s.Position.X, Y = s.Position.Y, Z = s.Position.Z, Orientation = s.Orientation
                };
                instance = new GameObjectSpawnInstance(goData, template);
                data = goData;
                spawnsContainer.AddExternalSpawn(instance, null, template);
            }

            await LoadSpawnAsync(instance, CancellationToken.None);

            // undone (or map changed) while the model was loading?
            if (!editService.NewSpawns.Any(n => n.IsCreature == s.IsCreature && n.Guid == s.Guid))
            {
                RemoveSpawnFromWorld(instance);
                return;
            }
            // unsaved spawn: dither until Save commits it (the refresh pass only runs on revision
            // changes, which this async load completes after)
            instance.WorldObject?.SetTranslucent(true);
            pendingSpawnInstances[key] = (instance, data);
        }
        finally
        {
            pendingSpawnLoads.Remove(key);
        }
    }

    private void ApplyPendingTransform((SpawnInstance instance, object data) live, PendingSpawn s)
    {
        if (live.data is PendingCreatureData cd)
        {
            cd.X = s.Position.X; cd.Y = s.Position.Y; cd.Z = s.Position.Z; cd.O = s.Orientation;
        }
        else if (live.data is PendingGameObjectData gd)
        {
            gd.X = s.Position.X; gd.Y = s.Position.Y; gd.Z = s.Position.Z; gd.Orientation = s.Orientation;
        }

        if (live.instance.WorldObject is not { } worldObject)
            return;
        worldObject.Position = s.Position;
        if (live.instance is CreatureSpawnInstance { Creature: { } creature })
            creature.Orientation = s.Orientation;
        else if (live.instance is GameObjectSpawnInstance { GameObject: { } gameObject })
            gameObject.Rotation = Quaternion.CreateFromAxisAngle(Vectors.Up, s.Orientation);
    }

    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();

    private SpawnInstance? FindSpawn(bool isCreature, uint guid)
    {
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            if (spawn is CreatureSpawnInstance == isCreature && spawn.Guid == guid)
                return spawn;
        }
        return null;
    }

    private void RemoveSpawnFromWorld(SpawnInstance spawn)
    {
        if (ReferenceEquals(spawnSelectionService.SelectedSpawn.Value, spawn))
            spawnSelectionService.SelectedSpawn.Value = null;
        spawnsContainer.RemoveExternalSpawn(spawn);
        if (spawn.IsSpawned)
            spawn.Dispose();
    }

    // pending-delete AND pending-new (unsaved) spawns dither, so all not-yet-committed world state
    // is legible at a glance (safe per-spawn: world object instances own per-instance material
    // clones, see WorldObjectInstance.OwnMaterial)
    private void RefreshPendingEditVisuals()
    {
        var newSpawns = editService.NewSpawns;
        HashSet<(bool isCreature, uint guid)>? pendingNew = newSpawns.Count > 0
            ? newSpawns.Select(s => (s.IsCreature, s.Guid)).ToHashSet()
            : null;
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            bool isCreature = spawn is CreatureSpawnInstance;
            spawn.WorldObject?.SetTranslucent(editService.IsPendingDelete(isCreature, spawn.Guid)
                || (pendingNew?.Contains((isCreature, spawn.Guid)) ?? false));
        }
    }

    private int? loadedMap;
    private void UpdateSpawnsData()
    {
        if (loadedMap.HasValue && loadedMap.Value == gameContext.CurrentMapId)
            return;

        loadedMap = (int)gameContext.CurrentMapId;
        LoadSpawnDataCoroutine(loadedMap.Value).FireAndForget();
    }

    private async ValueTask LoadSpawnDataCoroutine(int mapId)
    {
        while (spawnsContainer.IsLoading && gameContext.CurrentMapId == mapId)
            await engine.NextFrame;

        if (gameContext.CurrentMapId != mapId) // could have changed while waiting
            return;
        
        spawnsContainer.LoadMap(mapId);
    }
    
    private void RefreshVisibility()
    {
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            if (!spawn.IsSpawned)
                continue;
            
            spawn.WorldObject!.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) &&
                                                 spawn.IsVisibleInEvents(gameEventService);
        }
    }

    private void UpdateSelectionDecal()
    {
        if (!selectionDecalCreated)
            return;

        var selected = spawnSelectionService.SelectedSpawn.Value;
        ref var decal = ref entityManager.GetComponent<Decal>(selectionDecal);
        decal.Disabled = selected == null;
        if (selected == null)
            return;

        var center = selected.WorldObject?.Position ?? selected.Position;
        float radius = SelectionRadiusForSpawn(selected);
        entityManager.GetComponent<LocalToWorld>(selectionDecal).Matrix =
            Matrix.CreateScale(radius, radius, SelectionDecalVerticalHalf) *
            Matrix.CreateTranslation(center);
    }

    private float SelectionRadiusForSpawn(SpawnInstance spawn)
    {
        var worldObject = spawn.WorldObject;
        if (worldObject != null)
        {
            var entity = worldObject.WorldObjectEntity;
            if (entityManager.Exist(entity) && entityManager.HasComponent<WorldMeshBounds>(entity))
            {
                var box = entityManager.GetComponent<WorldMeshBounds>(entity).box;
                float horizontalHalf = 0.5f * MathF.Max(box.Size.X, box.Size.Y);
                return Math.Clamp(horizontalHalf * SelectionRadiusPadding, SelectionRadiusMin, SelectionRadiusMax);
            }
        }
        return SelectionRadiusFallback;
    }

    private ITexture BuildSelectionCircleTexture()
    {
        const int size = 256;
        var pixels = new Vector4[size * size];
        for (int y = 0; y < size; ++y)
        {
            for (int x = 0; x < size; ++x)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float d = MathF.Sqrt(u * u + v * v);
                float toCentre = SmoothStep(0f, 1f, Math.Clamp(d, 0f, 1f));
                float outerCut = 1f - SmoothStep(0.9f, 1f, d);
                pixels[y * size + x] = new Vector4(1f, 1f, 1f, toCentre * outerCut);
            }
        }
        return gameContext.Engine.TextureManager.CreateTexture(pixels, size, size);
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    public async ValueTask LoadChunk(int mapId, int chunkX, int chunkZ, CancellationToken cancellationToken)
    {
        while ((spawnsContainer.IsLoading || spawnsContainer.LoadedMap != mapId)
               && !cancellationToken.IsCancellationRequested)
            await engine.NextFrame;

        if (cancellationToken.IsCancellationRequested)
            return;

        // snapshot: a spawn placed from the picker while this coroutine is suspended would otherwise
        // invalidate the enumerator (AddExternalSpawn appends to the same per-chunk list)
        foreach (var spawn in spawnsContainer.SpawnsPerChunk[chunkX, chunkZ]!.ToArray())
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            await LoadSpawnAsync(spawn, cancellationToken);
        }
    }

    /// <summary>Loads the world model for a spawn (shared by chunk loading and session-placed spawns).</summary>
    private async ValueTask LoadSpawnAsync(SpawnInstance spawn, CancellationToken cancellationToken)
    {
        if (spawn.IsSpawned)
            return; // already loaded (e.g. placed this session, then its chunk load ran too)

        if (spawn is CreatureSpawnInstance creatureSpawnInstance)
        {
            var template = databaseProvider.GetCachedCreatureTemplate(spawn.Entry);

            if (template == null)
                return;

            var creatureInstance = new CreatureInstance(gameContext, template, null, renderLayer);

            await creatureInstance.Load();

            if (cancellationToken.IsCancellationRequested)
                return;

            creatureInstance.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) &&
                                               spawn.IsVisibleInEvents(gameEventService);
            creatureSpawnInstance.Creature = creatureInstance;
            creatureInstance.SetTranslucent(editService.IsPendingDelete(true, spawn.Guid));

            creatureInstance.Position = creatureSpawnInstance.Position;
            creatureInstance.Orientation = creatureSpawnInstance.Orientation;
            creatureInstance.Animation = M2AnimationType.Stand;

            if (creatureSpawnInstance.Equipment is { } eq)
            {
                await creatureInstance.SetVirtualItem(0, eq.Item1, cancellationToken);
                await creatureInstance.SetVirtualItem(1, eq.Item2, cancellationToken);
                await creatureInstance.SetVirtualItem(2, eq.Item3, cancellationToken);
            }

            // the awaits above yield frames; a map change meanwhile disposes the spawn
            // (destroying the creature's entities) and cancels the token
            if (cancellationToken.IsCancellationRequested)
                return;

            if (creatureSpawnInstance.Addon is {} addon)
            {
                creatureInstance.Animation = animationSystem.GetAnimationType(creatureInstance.Model,
                    addon.Emote, addon.StandState, (AnimTier)addon.AnimTier) ?? M2AnimationType.Stand;

                if (addon.Mount != 0)
                {
                    var mountModel = await mdxManager.LoadCreatureModel(addon.Mount);
                    if (mountModel != null && !cancellationToken.IsCancellationRequested)
                        creatureInstance.Mount = mountModel;
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            entityManager.AddManagedComponent(creatureInstance.WorldObjectEntity, spawn);
            entityManager.SetParent(creatureInstance.WorldObjectEntity, EnsureCreatureEntryNode(spawn.Entry, template.Name));
        }
        else if (spawn is GameObjectSpawnInstance gameObjectSpawnInstance)
        {
            var template = databaseProvider.GetCachedGameObjectTemplate(spawn.Entry);

            if (template == null)
                return;

            var gameobjectInstance = new GameObjectInstance(gameContext, template, null, renderLayer);

            await gameobjectInstance.Load();

            if (cancellationToken.IsCancellationRequested)
                return;

            gameobjectInstance.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) &&
                                                 spawn.IsVisibleInEvents(gameEventService);
            gameObjectSpawnInstance.GameObject = gameobjectInstance;
            gameobjectInstance.SetTranslucent(editService.IsPendingDelete(false, spawn.Guid));

            gameobjectInstance.Position = gameObjectSpawnInstance.Position;
            gameobjectInstance.Rotation = gameObjectSpawnInstance.Rotation;

            entityManager.AddManagedComponent(gameobjectInstance.WorldObjectEntity, spawn);
            entityManager.SetParent(gameobjectInstance.WorldObjectEntity, EnsureGameObjectEntryNode(spawn.Entry, template.Name));
        }
    }

    public async ValueTask UnloadChunk(int chunkX, int chunkZ)
    {
        foreach (var spawn in spawnsContainer.SpawnsPerChunk[chunkX, chunkZ]!)
        {
            if (spawn.IsSpawned)
                spawn.Dispose();
        }
    }
}