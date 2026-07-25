using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Waypoints;
using WDE.MapSpawns.ViewModels;
using Key = TheEngine.Input.Key;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawns.Rendering.Waypoints;

/// <summary>
/// Orchestrates the waypoints editor: the inspector section (via <see cref="WaypointInspector"/>),
/// the world rendering (via <see cref="WaypointRenderStage"/>), the per-frame world interaction
/// (ImGuizmo drag, ray picking, insert-on-segment, append-on-terrain) and the creature-driven
/// auto-load/unload with a save prompt.
/// </summary>
public class WaypointEditorModule : IGameModule
{
    // a point/segment is "hit" if the cursor ray passes within this fraction of the distance to it
    private const float PickAngularThreshold = 0.018f;

    private readonly Engine engine;
    private readonly IGameContext gameContext;
    private readonly IWaypointEditorService service;
    private readonly ISpawnEditorToolService toolService;
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly RaycastSystem raycastSystem;
    private readonly IInputManager inputManager;
    private readonly IEntityManager entityManager;
    private readonly IWorldInteractionService interaction;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly IGameViewOverlayService overlays;
    private readonly WaypointInspector inspector;

    private WaypointRenderStage? renderStage;
    private bool renderStageRegistered;

    private Entity projectorDecal = Entity.Empty;
    private bool projectorDecalCreated;

    public object? ViewModel => null;

    // the read-only route preview shown for the selected creature (Select tool etc.) - this is NOT
    // loaded into the editor; editing a path is always explicit (right-click / Load / import)
    private static readonly Vector4 SelectionPreviewColor = new(1.0f, 0.6f, 0.15f, 0.9f);
    private SpawnInstance? lastSelection;
    private WaypointSource previewSource;
    private uint previewKey;
    private Task<IReadOnlyList<Vector3>?>? previewTask;
    private WaypointPreviewPath? selectionPreview;

    private EditablePath? dragPath;
    private int dragIndex = -1;
    private CaptureLease lease;

    // G-key grab of the selected point - the shared spawn-dragger state machine (G grabs, G again
    // snaps to ground, click drops, Escape cancels; points have no yaw, so R stays inert here)
    private readonly TransformDragger pointDragger;
    private readonly WaypointDragTarget pointDragTarget = new();

    private sealed class WaypointDragTarget : IDragTarget
    {
        private EditablePath? path;
        private int index;

        public void Set(EditablePath p, int i)
        {
            path = p;
            index = i;
        }

        public Vector3 Position
        {
            get => path != null && index >= 0 && index < path.Points.Count ? Pos(path.Points[index]) : Vector3.Zero;
            set
            {
                if (path != null && index >= 0 && index < path.Points.Count)
                    path.SetPosition(index, value);
            }
        }

        public bool HasOrientation => false;

        public float Orientation
        {
            get => 0f;
            set { }
        }
    }

    public WaypointEditorModule(Engine engine,
        IGameContext gameContext,
        IWaypointEditorService service,
        ISpawnEditorToolService toolService,
        ISpawnSelectionService spawnSelectionService,
        RaycastSystem raycastSystem,
        IInputManager inputManager,
        IEntityManager entityManager,
        IWorldInteractionService interaction,
        ISpawnsContainer spawnsContainer,
        IGameViewOverlayService overlays,
        ISpawnScriptsService spawnScriptsService,
        IGameNotificationService notifications)
    {
        this.engine = engine;
        this.gameContext = gameContext;
        this.service = service;
        this.toolService = toolService;
        this.spawnSelectionService = spawnSelectionService;
        this.raycastSystem = raycastSystem;
        this.inputManager = inputManager;
        this.entityManager = entityManager;
        this.interaction = interaction;
        this.spawnsContainer = spawnsContainer;
        this.overlays = overlays;
        inspector = new WaypointInspector(service, spawnScriptsService, notifications);
        pointDragger = new TransformDragger(engine, inputManager, raycastSystem, interaction, toolService);
    }

    private void ReleaseCapture()
    {
        if (lease.IsActive)
            lease.Release();
        lease = default;
    }

    public void Initialize()
    {
        renderStage = new WaypointRenderStage(engine, service, toolService);
        overlays.SetSection(SpawnEditorTool.Waypoint, inspector);
        // NB: the render stage is registered on the first Update (see below), not here. Stages render
        // in registration order, and every game module's Initialize runs before any module's first
        // Update (see ModuleManager.Update) - so registering on first Update appends our stage after
        // all the other modules' stages, i.e. the waypoint overlay draws last, as requested.
    }

    public void Dispose()
    {
        ReleaseCapture();
        ClearPathIcons();
        ClearSelectionPreview();
        overlays.SetSection(SpawnEditorTool.Waypoint, null);
        if (renderStage != null)
        {
            engine.RenderManager.UnregisterRenderStage(renderStage);
            renderStage.Dispose();
            renderStage = null;
        }
        if (projectorDecalCreated && entityManager.Exist(projectorDecal))
            entityManager.DestroyEntity(projectorDecal);
        projectorDecalCreated = false;
    }

    public void Update(float delta)
    {
        // register the render stage last (see Initialize) - all other modules are initialized by now
        if (renderStage != null && !renderStageRegistered)
        {
            engine.RenderManager.RegisterRenderStage(renderStage);
            renderStageRegistered = true;
        }

        service.PumpPendingLoads();
        UpdateSelectionPreview();
        ProcessCreatureActions();
        ProcessImportRequests();
        UpdateProjectorDecal();
        RefreshPathIcons(delta);

        inspector.DragHint = pointDragger.ActiveHint; // state-sensitive hint bar during a point grab

        if (toolService.ActiveTool != SpawnEditorTool.Waypoint)
        {
            CancelPointDrag();
            service.EditingPath = null; // leaving the tool always disarms point-adding
            return;
        }

        if (interaction.IsCaptured && !lease.IsActive && !pointDragger.IsActive)
            return;

        // P toggles edit (pen) mode on the selected path (E/Q belong to the camera, F is the
        // global frame-selected); Escape steps out innermost-first: pen mode, then the point
        // selection - the spawn-level escape chain (leave the tool, deselect the spawn) stands
        // down while either exists, and an open ImGui popup owns Escape entirely (it closes
        // itself, see ImGuiEx.BeginPopup). Appending on terrain is gated on this explicit mode
        // so a click that misses a point can never grow the path by accident.
        if (!pointDragger.IsActive && dragPath == null)
        {
            if (inputManager.Keyboard.JustPressed(Key.P) && service.SelectedPath is { } toToggle)
                service.EditingPath = ReferenceEquals(service.EditingPath, toToggle) ? null : toToggle;
            else if (inputManager.Keyboard.JustPressed(Key.Escape) && !ImGuiEx.AnyPopupOpen)
            {
                if (service.EditingPath != null)
                    service.EditingPath = null;
                else if (SelectedPoint() != null)
                    service.SelectedPointIndex = -1;
            }
        }

        // Delete removes the selected point (the table's per-row "x" is demoted to an expander now).
        // Back(space) too: on macOS the physical delete key reports as Backspace.
        if (!pointDragger.IsActive && dragPath == null && SelectedPoint() is { } toRemove &&
            (inputManager.Keyboard.JustPressed(Key.Delete) || inputManager.Keyboard.JustPressed(Key.Back)))
        {
            toRemove.path.RemoveAt(toRemove.index);
            if (service.SelectedPointIndex >= toRemove.path.Points.Count)
                service.SelectedPointIndex = toRemove.path.Points.Count - 1;
            return;
        }

        // G grabs the selected point - the shared dragger owns the grab/snap/drop/cancel semantics
        if (dragPath == null && SelectedPoint() is { } sel)
        {
            pointDragTarget.Set(sel.path, sel.index);
            if (pointDragger.Update(pointDragTarget))
                return;
        }

        if (dragPath == null && toolService.ShowGizmoHandles && GizmoMoveSelectedPoint())
            return;
        if (ImGuizmo.IsUsing())
            return;

        if (dragPath != null)
        {
            if (inputManager.Mouse.IsMouseDown(MouseButton.Left))
            {
                var hit = raycastSystem.RaycastMouse(Collisions.COLLISION_MASK_STATIC);
                if (hit.HasValue)
                    dragPath.SetPosition(dragIndex, hit.Value.Item2);
            }
            else
            {
                dragPath = null;
                dragIndex = -1;
                ReleaseCapture();
            }
            return;
        }

        UpdateEditModePreview();

        if (!inputManager.Mouse.HasJustClicked(MouseButton.Left))
            return;

        var ray = engine.CameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);

        if (PickPoint(ray, out var pickedPath, out var pickedIndex))
        {
            service.SelectedPath = pickedPath;
            service.SelectedPointIndex = pickedIndex;
            interaction.UsePointerThisFrame();
            if (inputManager.Mouse.HasJustDoubleClicked)
                inspector.OpenProperties(pickedPath, pickedIndex);
            return;
        }

        if (PickSegment(ray, out var segPath, out var segInsertIndex, out var onSegment))
        {
            // clicking a line always inserts a point in-between (a deliberate, precise action -
            // unlike terrain appends, which stay gated behind the explicit pen mode)
            segPath.InsertAt(segInsertIndex, onSegment);
            service.SelectedPath = segPath;
            service.SelectedPointIndex = segInsertIndex;
            dragPath = segPath;
            dragIndex = segInsertIndex;
            interaction.TryCapture(out lease);
            interaction.UsePointerThisFrame();
            return;
        }

        // pen behavior only while the path is explicitly in edit mode: clicking terrain appends.
        // Outside edit mode a terrain click just clears the point selection.
        if (service.EditingPath is { } editPath)
        {
            var hit = raycastSystem.RaycastMouse(Collisions.COLLISION_MASK_STATIC);
            if (hit.HasValue)
            {
                var idx = editPath.Append(hit.Value.Item2);
                service.SelectedPointIndex = idx;
                interaction.UsePointerThisFrame();
            }
        }
        else
        {
            service.SelectedPointIndex = -1;
        }
    }

    // Rubber band while in edit mode: a line from the path's last point to the cursor's ground hit
    // plus a marker there - you SEE what a click will add before clicking. The raycast runs in
    // Update; the actual draws happen in Render (DrawSphere is render-loop-only).
    private bool previewValid;
    private Vector3 previewTarget;
    private Vector3 previewFrom;
    private bool previewHasFrom;

    private void UpdateEditModePreview()
    {
        previewValid = false;
        if (service.EditingPath is not { } path || engine.GameView.IsPointerOverOverlay)
            return;

        var hit = raycastSystem.RaycastMouse(Collisions.COLLISION_MASK_STATIC);
        if (!hit.HasValue)
            return;

        previewTarget = hit.Value.Item2;
        previewHasFrom = path.Points.Count > 0;
        if (previewHasFrom)
            previewFrom = Pos(path.Points[^1]);
        previewValid = true;
    }

    public void Render(float delta)
    {
        if (!previewValid || toolService.ActiveTool != SpawnEditorTool.Waypoint || service.EditingPath == null)
            return;
        var color = new Vector4(0.35f, 1.0f, 0.45f, 0.9f);
        if (previewHasFrom)
            engine.RenderManager.DrawLine(previewFrom, previewTarget, color);
        engine.RenderManager.DrawSphere(previewTarget, 0.35f, color);
    }

    public void RenderGUI()
    {
        // the popups (load picker, SQL preview, point properties popover) draw regardless of the
        // active tool - a world double-click may open the popover from Select mode transitions etc.
        inspector.DrawPopups();
        DrawWorldBadges();
        pointDragger.DrawGuides(); // axis-lock guide line while a point grab is constrained
    }

    // Screen-space chips over the selected path's points: the selected/hovered point's index and a
    // wait-time label on every point with a delay - the data that used to require the point table.
    private void DrawWorldBadges()
    {
        if (toolService.ActiveTool != SpawnEditorTool.Waypoint || service.SelectedPath is not { } path || path.Points.Count == 0)
            return;

        if (!ImGui.Begin("3D"))
        {
            ImGui.End();
            return;
        }

        var dl = ImGui.GetWindowDrawList();
        var view = engine.GameView.ViewRect;
        var camera = engine.CameraManager.MainCamera;
        var viewProj = camera.ViewMatrix * camera.ProjectionMatrix;

        int hoveredIndex = -1;
        if (!engine.GameView.IsPointerOverOverlay)
        {
            var ray = camera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);
            if (PickPoint(ray, out var hoveredPath, out var idx) && ReferenceEquals(hoveredPath, path))
                hoveredIndex = idx;
        }

        for (int i = 0; i < path.Points.Count; ++i)
        {
            var p = path.Points[i];
            uint delay = p.Delay ?? 0;
            bool emphasized = i == service.SelectedPointIndex || i == hoveredIndex;
            if (delay == 0 && !emphasized)
                continue;

            var clip = Vector4.Transform(new Vector4(p.X, p.Y, p.Z, 1f), viewProj);
            if (clip.W <= 0)
                continue;
            float nx = (clip.X / clip.W + 1f) * 0.5f;
            float ny = 1f - (clip.Y / clip.W + 1f) * 0.5f;
            if (nx < 0 || nx > 1 || ny < 0 || ny > 1)
                continue;

            string label = delay > 0 ? $"#{i + 1} · {delay / 1000f:0.#}s" : $"#{i + 1}";
            var textSize = ImGui.CalcTextSize(label);
            var pad = new System.Numerics.Vector2(5, 2);
            var anchor = new System.Numerics.Vector2(view.X + nx * view.Width, view.Y + ny * view.Height);
            var min = anchor + new System.Numerics.Vector2(-textSize.X * 0.5f - pad.X, -textSize.Y - 14 - pad.Y * 2);
            var max = min + textSize + pad * 2;

            dl.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.WindowBg, emphasized ? 0.95f : 0.70f), 4f);
            if (emphasized)
                dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Text, 0.7f), 4f);
            dl.AddText(min + pad, ImGui.GetColorU32(ImGuiCol.Text, emphasized ? 1f : 0.85f), label);
        }

        ImGui.End();
    }

    // "has waypoints" overhead icon (StatusIconsManager, like the quest !/? markers): every creature
    // whose spawn resolves to a movement path gets a green route glyph above its head. Recomputed on a
    // throttle rather than every frame - the owner set only shifts as spawns stream in/out or paths are
    // attached/detached, none of which needs frame-accurate tracking.
    private const float PathIconRefreshInterval = 0.5f;
    private float pathIconRefreshTimer;
    private bool pathIconsPushed;
    private readonly List<CreatureInstance> pathOwnersScratch = new();
    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();
    // cached query archetype: the entities carrying a SpawnInstance managed component ARE exactly the
    // spawns currently streamed into the world (added on load, gone when the entity is destroyed on
    // unload), so the ECS itself is the loaded-spawn registry - no separate bookkeeping to keep in sync
    private Archetype? spawnQueryArchetype;

    private void RefreshPathIcons(float delta)
    {
        // core has no creature<->path attachment convention -> nothing to indicate (feature hidden)
        if (!service.SupportsCreaturePaths)
            return;

        pathIconRefreshTimer -= delta;
        if (pathIconRefreshTimer > 0)
            return;
        pathIconRefreshTimer = PathIconRefreshInterval;

        // Iterate only the spawns actually streamed into the world (the entities that carry a
        // SpawnInstance managed component) rather than walking every spawn on the map - the old
        // whole-map walk (tens of thousands, 99% in unloaded chunks) was eating most of a frame.
        spawnQueryArchetype ??= entityManager.NewArchetype().WithManagedComponentData<SpawnInstance>();
        pathOwnersScratch.Clear();
        var itr = entityManager.ArchetypeIterator(spawnQueryArchetype);
        while (itr.MoveNext())
        {
            var chunk = itr.Current;
            var spawns = chunk.ManagedDataAccess<SpawnInstance>();
            for (int i = 0; i < chunk.Length; ++i)
            {
                if (spawns[i] is CreatureSpawnInstance creature && creature.Creature is { } instance &&
                    service.ResolveCreaturePath(creature) != null)
                    pathOwnersScratch.Add(instance);
            }
        }
        gameContext.StatusIconsManager.SetDynamicIconOwners(StatusIconsManager.StatusIcon.Waypoints, pathOwnersScratch);
        pathIconsPushed = pathOwnersScratch.Count > 0;
    }

    private void ClearPathIcons()
    {
        if (!pathIconsPushed)
            return;
        pathOwnersScratch.Clear();
        gameContext.StatusIconsManager.SetDynamicIconOwners(StatusIconsManager.StatusIcon.Waypoints, pathOwnersScratch);
        pathIconsPushed = false;
    }

    private void UpdateProjectorDecal()
    {
        var projectorTexture = renderStage?.ProjectorTexture;
        // the render stage stops repainting the projector RT when the Waypoint tool isn't active, so
        // the texture would keep its last-rendered path - gate the decal on the tool too, otherwise a
        // stale ground path lingers after switching tools.
        bool active = service.SelectedPath is { Points.Count: > 0 } && projectorTexture != null
                      && toolService.ActiveTool == SpawnEditorTool.Waypoint;

        if (!projectorDecalCreated)
        {
            if (!active)
                return; // nothing to project yet - don't spawn the entity until it's needed
            projectorDecal = entityManager.CreateEntity(entityManager.NewArchetype()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<Decal>(), "Waypoint path projector");
            projectorDecalCreated = true;
        }

        WaypointProjector.ComputeBox(service.SelectedPath, out var boxCenter, out var boxRadius, out var boxVHalf);
        entityManager.GetComponent<LocalToWorld>(projectorDecal).Matrix =
            WaypointProjector.ComputeDecalLocalToWorld(boxCenter, boxRadius, boxVHalf);

        ref var decal = ref entityManager.GetComponent<Decal>(projectorDecal);
        decal.Disabled = !active;
        // Shadow look: black color means the decal blend mix(albedo, rgb*color.rgb=0, alpha) becomes
        // albedo*(1-alpha) - i.e. it DARKENS the underlying surface color instead of tinting it. The
        // alpha (.w) is the darken strength on flat ground (~0.55 = up to 55% darker).
        decal.Color = new Vector4(0f, 0f, 0f, 0.55f);
        decal.FadeAngleCos = 0.3f;
        if (!ReferenceEquals(decal.Albedo, projectorTexture))
            decal.Albedo = projectorTexture; // setter manages the GC handle; changes ~once
    }

    // Selecting a creature shows its route as a READ-ONLY overlay (visible in the Select tool too),
    // never loading it into the waypoint editor - editing a path stays an explicit action (the
    // right-click "Edit waypoints" menu, the Load button, or an import).
    private void UpdateSelectionPreview()
    {
        var sel = spawnSelectionService.SelectedSpawn.Value;
        if (!ReferenceEquals(sel, lastSelection))
        {
            lastSelection = sel;
            ClearSelectionPreview();

            if (service.SupportsCreaturePaths && sel is CreatureSpawnInstance creature &&
                service.ResolveCreaturePath(creature) is { } attached)
            {
                previewSource = attached.source;
                previewKey = attached.key;
                previewTask = service.LoadPointsPreview(attached.source, attached.key);
            }
        }

        PollPreviewLoad();

        // if the previewed path just became the one open for editing, drop the duplicate overlay
        if (selectionPreview != null && service.SelectedPath is { } cur &&
            cur.Source == previewSource && cur.Key == previewKey)
            ClearSelectionPreview();
    }

    private void PollPreviewLoad()
    {
        if (previewTask is not { IsCompleted: true } t)
            return;
        var points = t.IsCompletedSuccessfully ? t.Result : null;
        previewTask = null;
        if (points == null || points.Count < 2)
            return;
        // don't preview a path that's already open for editing (it's drawn by the editor itself)
        if (service.SelectedPath is { } cur && cur.Source == previewSource && cur.Key == previewKey)
            return;
        selectionPreview = new WaypointPreviewPath { Points = points, Color = SelectionPreviewColor };
        service.PreviewPaths.Add(selectionPreview);
    }

    private void ClearSelectionPreview()
    {
        if (selectionPreview != null)
        {
            service.PreviewPaths.Remove(selectionPreview);
            selectionPreview = null;
        }
        previewTask = null;
    }

    // add/edit/remove-waypoints requests queued by the spawn context menu (UI thread) - executed
    // here on the engine thread, which owns the editor state
    private void ProcessCreatureActions()
    {
        while (service.TryDequeueCreatureAction(out var creature, out var action))
        {
            Console.WriteLine($"[Waypoints] processing {action} for creature {creature.Guid}");
            switch (action)
            {
                case CreaturePathAction.Remove:
                    service.RemoveCreaturePath(creature).ListenErrors();
                    break;
                case CreaturePathAction.EditTemplate:
                    EditCreatureTemplatePath(creature).ListenErrors();
                    break;
                default:
                    EditCreaturePath(creature).ListenErrors();
                    break;
            }
        }
    }

    private async Task EditCreaturePath(CreatureSpawnInstance creature)
    {
        var path = await service.AttachOrLoadCreaturePath(creature);
        if (path == null)
        {
            Console.WriteLine($"[Waypoints] no path could be attached/loaded for creature {creature.Guid}");
            return;
        }
        OpenPathForEditing(path);
    }

    private async Task EditCreatureTemplatePath(CreatureSpawnInstance creature)
    {
        var path = await service.EditOrLoadCreatureTemplatePath(creature);
        if (path == null)
        {
            Console.WriteLine($"[Waypoints] no template path could be loaded for creature entry {creature.Entry}");
            return;
        }
        OpenPathForEditing(path);
    }

    private void OpenPathForEditing(EditablePath path)
    {
        Console.WriteLine($"[Waypoints] opening editor: {path.DisplayName}, {path.Points.Count} point(s)");
        service.SelectedPath = path;
        // a brand new (empty) path is meant to be drawn right away - arm edit mode; an existing
        // path opens for viewing/tweaking, the user arms it explicitly (E / panel button)
        if (path.Points.Count == 0)
            service.EditingPath = path;
        toolService.ActiveTool = SpawnEditorTool.Waypoint;
    }

    // externally-sourced paths (sniffed movement) queued from any thread - imported here on the
    // engine thread as regular editable paths
    private void ProcessImportRequests()
    {
        while (service.TryDequeueImport(out var request))
            ImportPath(request).ListenErrors();
    }

    private async Task ImportPath(WaypointImportRequest request)
    {
        if (request.Points.Count == 0)
            return;

        // attach to the matching creature spawn when one stands where the path expects it
        CreatureSpawnInstance? attachTo = null;
        if (request.AttachEntry != 0 && service.SupportsCreaturePaths)
            attachTo = FindNearestCreature(request.AttachEntry, request.AttachHint, maxDistance: 25f);

        var path = attachTo != null ? await service.AttachOrLoadCreaturePath(attachTo) : null;
        path ??= await service.CreateNewStandalone();
        if (path == null)
        {
            Console.WriteLine("[Waypoints] import failed - the core has no usable waypoint source");
            return;
        }

        // import REPLACES the path's points (nothing hits the DB until Save)
        path.Points.Clear();
        path.Points.AddRange(request.Points);
        path.Reindex();
        path.MarkDirty();

        Console.WriteLine($"[Waypoints] imported {request.Points.Count} point(s) into {path.DisplayName}"
                          + (attachTo != null ? $" (attached to creature {attachTo.Guid})" : " (standalone)"));
        service.SelectedPath = path;
        service.SelectedPointIndex = -1;
        toolService.ActiveTool = SpawnEditorTool.Waypoint;
    }

    private CreatureSpawnInstance? FindNearestCreature(uint entry, Vector3 near, float maxDistance)
    {
        CreatureSpawnInstance? best = null;
        float bestDistance = maxDistance;
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            if (spawn is not CreatureSpawnInstance creature || creature.Entry != entry || creature.WorldObject == null)
                continue;
            var distance = (creature.WorldObject.Position - near).Length();
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = creature;
            }
        }
        return best;
    }

    private static Vector3 Pos(UniversalWaypoint w) => new(w.X, w.Y, w.Z);

    // --- G-key plane drag of the selected point --------------------------------------------------

    /// <summary>The currently selected point, or null when the selection is stale (path unloaded,
    /// point removed...).</summary>
    private (EditablePath path, int index)? SelectedPoint()
    {
        var path = service.SelectedPath;
        int idx = service.SelectedPointIndex;
        if (path == null || idx < 0 || idx >= path.Points.Count)
            return null;
        return (path, idx);
    }

    /// <summary>Cancels an in-flight G-drag, restoring the point when the selection still resolves.</summary>
    private void CancelPointDrag()
    {
        if (!pointDragger.IsActive)
        {
            pointDragger.Cancel(null); // keeps the gizmo-mode mirror in sync while inactive
            return;
        }
        if (SelectedPoint() is { } sel)
            pointDragTarget.Set(sel.path, sel.index);
        pointDragger.Cancel(SelectedPoint() != null ? pointDragTarget : null);
    }

    private bool PickPoint(Ray ray, out EditablePath path, out int index)
    {
        path = null!;
        index = -1;
        float bestScore = float.MaxValue;

        if (service.SelectedPath is not { } p)
            return false;

        for (int i = 0; i < p.Points.Count; ++i)
        {
            var point = Pos(p.Points[i]);
            var toP = point - ray.Position;
            float t = Vector3.Dot(toP, ray.Direction);
            if (t <= 0)
                continue;
            var closest = ray.Position + ray.Direction * t;
            float dist = (point - closest).Length();
            float threshold = PickAngularThreshold * t;
            if (dist < threshold && dist < bestScore)
            {
                bestScore = dist;
                path = p;
                index = i;
            }
        }

        return index >= 0;
    }

    private bool PickSegment(Ray ray, out EditablePath path, out int insertIndex, out Vector3 onSegment)
    {
        path = null!;
        insertIndex = -1;
        onSegment = default;
        float bestDist = float.MaxValue;

        if (service.SelectedPath is not { } p)
            return false;

        for (int i = 0; i < p.Points.Count - 1; ++i)
        {
            var a = Pos(p.Points[i]);
            var b = Pos(p.Points[i + 1]);
            var (dist, t, pointOnSeg) = ClosestRayToSegment(ray, a, b);
            if (t <= 0)
                continue;
            float threshold = PickAngularThreshold * t;
            if (dist < threshold && dist < bestDist)
            {
                bestDist = dist;
                path = p;
                insertIndex = i + 1;
                onSegment = pointOnSeg;
            }
        }

        return insertIndex >= 0;
    }

    private static (float dist, float t, Vector3 pointOnSeg) ClosestRayToSegment(Ray ray, Vector3 a, Vector3 b)
    {
        var d1 = ray.Direction;
        var d2 = b - a;
        var r = ray.Position - a;
        float aa = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);
        float c = Vector3.Dot(d1, r);
        float bb = Vector3.Dot(d1, d2);
        float denom = aa * e - bb * bb;

        float t = denom > 1e-5f ? (bb * f - c * e) / denom : 0f;
        float s = e > 1e-5f ? (bb * t + f) / e : 0f;
        s = Math.Clamp(s, 0f, 1f);
        t = Math.Max(0f, (bb * s - c) / Math.Max(aa, 1e-5f));

        var rayPoint = ray.Position + d1 * t;
        var segPoint = a + d2 * s;
        return ((rayPoint - segPoint).Length(), t, segPoint);
    }

    private static unsafe Span<float> AsSpan(ref Matrix4x4 matrix) =>
        MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix4x4, float>(ref matrix), 16);

    private unsafe bool GizmoMoveSelectedPoint()
    {
        var path = service.SelectedPath;
        int idx = service.SelectedPointIndex;
        if (path == null || idx < 0 || idx >= path.Points.Count)
            return false;

        var view = engine.CameraManager.MainCamera.ViewMatrix;
        var proj = engine.CameraManager.MainCamera.ProjectionMatrix;
        var pos = Pos(path.Points[idx]);
        var local = Matrix4x4.CreateTranslation(pos.X, pos.Y, pos.Z);

        // distinct id so this gizmo doesn't fight the spawn gizmo (default id 0) when both a
        // creature and a waypoint point are selected at the same time
        ImGuizmo.SetID(1);
        try
        {
            fixed (float* viewPtr = AsSpan(ref view))
            fixed (float* projPtr = AsSpan(ref proj))
            fixed (float* localPtr = AsSpan(ref local))
            {
                if (ImGuizmo.Manipulate(viewPtr, projPtr, ImGuizmoOperation.Translate, ImGuizmoMode.World, localPtr))
                {
                    var span = AsSpan(ref local);
                    path.SetPosition(idx, new Vector3(span[12], span[13], span[14]));
                    return true;
                }
            }
        }
        finally
        {
            ImGuizmo.SetID(0);
        }

        return false;
    }
}
