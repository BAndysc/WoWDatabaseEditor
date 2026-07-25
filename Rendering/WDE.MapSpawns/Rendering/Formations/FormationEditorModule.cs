using Hexa.NET.ImGui;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Formations;
using WDE.MapSpawns.ViewModels;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawns.Rendering.Formations;

public class FormationEditorModule : IGameModule
{
    private const float PickAngularThreshold = 0.02f;

    private readonly Engine engine;
    private readonly IFormationEditorService service;
    private readonly ISpawnEditorToolService toolService;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly RaycastSystem raycastSystem;
    private readonly IInputManager inputManager;
    private readonly IEntityManager entityManager;
    private readonly IRenderManager renderManager;
    private readonly IWorldInteractionService interaction;
    private readonly IGameViewOverlayService overlays;
    private readonly FormationInspector inspector;

    private FormationRenderStage? renderStage;
    private bool renderStageRegistered;

    private bool dragging;
    private uint dragMemberGuid;
    private uint dragTargetGuid;
    private CaptureLease lease;

    public object? ViewModel => null;

    public FormationEditorModule(Engine engine,
        IFormationEditorService service,
        ISpawnEditorToolService toolService,
        ISpawnsContainer spawnsContainer,
        RaycastSystem raycastSystem,
        IInputManager inputManager,
        IEntityManager entityManager,
        IRenderManager renderManager,
        IWorldInteractionService interaction,
        IGameViewOverlayService overlays)
    {
        this.engine = engine;
        this.service = service;
        this.toolService = toolService;
        this.spawnsContainer = spawnsContainer;
        this.raycastSystem = raycastSystem;
        this.inputManager = inputManager;
        this.entityManager = entityManager;
        this.renderManager = renderManager;
        this.interaction = interaction;
        this.overlays = overlays;
        inspector = new FormationInspector(service);
    }

    private void ReleaseCapture()
    {
        if (lease.IsActive)
            lease.Release();
        lease = default;
    }

    public void Initialize()
    {
        renderStage = new FormationRenderStage(engine, service);
        overlays.SetSection(SpawnEditorTool.Formation, inspector);
        // the render stage is registered on the first Update (see WaypointEditorModule) so it appends
        // after the other modules' stages and draws last.
    }

    public void Dispose()
    {
        ReleaseCapture();
        overlays.SetSection(SpawnEditorTool.Formation, null);
        if (renderStage != null)
        {
            engine.RenderManager.UnregisterRenderStage(renderStage);
            renderStage.Dispose();
            renderStage = null;
        }
    }

    public void Update(float delta)
    {
        if (renderStage != null && !renderStageRegistered)
        {
            engine.RenderManager.RegisterRenderStage(renderStage);
            renderStageRegistered = true;
        }

        service.PumpPendingLoads();
        SyncMap();
        service.SyncConstraints();

        // exclusivity: the formation tool is live only while it's the active tool
        service.ToolEnabled = toolService.ActiveTool == SpawnEditorTool.Formation;

        if (!service.ToolEnabled)
        {
            service.DragActive = false;
            dragging = false;
            ReleaseCapture();
            return;
        }

        if (service.Selected != null &&
            (inputManager.Keyboard.JustPressed(Key.Delete) || inputManager.Keyboard.JustPressed(Key.Back)))
        {
            service.Remove(service.Selected);
            return;
        }

        if (dragging)
        {
            UpdateDrag();
            return;
        }

        if (!inputManager.Mouse.HasJustClicked(MouseButton.Left))
            return;

        if (interaction.IsCaptured)
            return;

        var creature = PickCreatureUnderCursor();
        if (creature != null)
        {
            dragging = true;
            dragMemberGuid = creature.Guid;
            dragTargetGuid = 0;
            service.DragActive = true;
            service.DragFrom = LivePos(creature);
            service.DragTo = LivePos(creature);
            service.DragSnapped = false;
            interaction.TryCapture(out lease);
            interaction.UsePointerThisFrame();
            return;
        }

        var ray = engine.CameraManager.MainCamera.NormalizedScreenPointToRay(inputManager.Mouse.NormalizedPosition);
        if (PickArrow(ray, out var picked))
        {
            service.Selected = picked;
            interaction.UsePointerThisFrame();
            if (inputManager.Mouse.HasJustDoubleClicked)
                inspector.OpenProperties(picked);
        }
        else
        {
            service.Selected = null;
        }
    }

    private void UpdateDrag()
    {
        var target = PickCreatureUnderCursor();
        if (target != null && target.Guid != dragMemberGuid)
        {
            service.DragTo = LivePos(target);
            service.DragSnapped = true;
            dragTargetGuid = target.Guid;
        }
        else
        {
            var hit = raycastSystem.RaycastMouse(Collisions.COLLISION_MASK_STATIC);
            if (hit.HasValue)
                service.DragTo = hit.Value.Item2;
            service.DragSnapped = false;
            dragTargetGuid = 0;
        }

        if (inputManager.Mouse.HasJustReleased(MouseButton.Left))
        {
            if (dragTargetGuid != 0 && dragTargetGuid != dragMemberGuid)
                service.Add(dragMemberGuid, dragTargetGuid);
            dragging = false;
            service.DragActive = false;
            interaction.UsePointerThisFrame();
            ReleaseCapture();
        }
    }

    private void SyncMap()
    {
        if (spawnsContainer.IsLoading)
            return;
        var map = spawnsContainer.LoadedMap;
        if (!map.HasValue || map.Value == service.LoadedMap)
            return;
        service.LoadForMap(map.Value).ListenErrors();
    }

    // live entity position (LocalToWorld), so the rubber-band tracks objects that have been moved
    private static Vector3 LivePos(CreatureSpawnInstance creature) =>
        creature.WorldObject?.Position ?? creature.Position;

    private CreatureSpawnInstance? PickCreatureUnderCursor()
    {
        var picked = renderManager.PickObject(inputManager.Mouse.NormalizedPosition);
        if (picked.IsEmpty())
            return null;
        picked = picked.GetRoot(entityManager);
        if (!entityManager.Exist(picked))
            return null;
        if (!entityManager.HasManagedComponent<SpawnInstance>(picked))
            return null;
        return entityManager.GetManagedComponent<SpawnInstance>(picked) as CreatureSpawnInstance;
    }

    private bool PickArrow(Ray ray, out EditableFormation picked)
    {
        picked = null!;
        float bestDist = float.MaxValue;

        foreach (var f in service.LoadedFormations)
        {
            if (f.IsLeaderSelfRow)
                continue;
            if (!service.TryGetEndpoints(f, out var leaderPos, out var memberPos))
                continue;

            var (dist, t, _) = ClosestRayToSegment(ray, leaderPos, memberPos);
            if (t <= 0)
                continue;
            float threshold = PickAngularThreshold * t;
            if (dist < threshold && dist < bestDist)
            {
                bestDist = dist;
                picked = f;
            }
        }

        return picked != null;
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

    public void RenderGUI()
    {
        // the properties popover draws regardless of the active tool (world double-click)
        inspector.DrawPopups();
    }
}
