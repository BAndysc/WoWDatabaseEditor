using System;
using System.Collections.Generic;
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
using WDE.MapSpawns.Models.CreatureLinking;
using WDE.MapSpawns.ViewModels;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawns.Rendering.CreatureLinking;

/// <summary>
/// The Creature-linking tool: drag one creature onto another to link them (the dragged one is the
/// slave that reacts to the master's aggro/evade/death/respawn events). The active
/// <see cref="ICreatureLinkEditorService.Mode"/> decides whether the drop writes a guid link
/// (creature_linking) or an entry link (creature_linking_template, using the two creatures' entries
/// on the current map). Clicking an existing arrow selects its link; Del removes the selected link.
/// Arrow rendering + the panel live in <see cref="CreatureLinkRenderStage"/> and
/// <see cref="CreatureLinkInspector"/>.
/// </summary>
public class CreatureLinkEditorModule : IGameModule
{
    private const float PickAngularThreshold = 0.02f;

    private readonly Engine engine;
    private readonly ICreatureLinkEditorService service;
    private readonly ISpawnEditorToolService toolService;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly RaycastSystem raycastSystem;
    private readonly IInputManager inputManager;
    private readonly IEntityManager entityManager;
    private readonly IRenderManager renderManager;
    private readonly IWorldInteractionService interaction;
    private readonly IGameViewOverlayService overlays;
    private readonly CreatureLinkInspector inspector;

    private CreatureLinkRenderStage? renderStage;
    private bool renderStageRegistered;

    private bool dragging;
    private uint dragSlaveGuid;
    private uint dragTargetGuid;
    private CaptureLease lease;

    public object? ViewModel => null;

    public CreatureLinkEditorModule(Engine engine,
        ICreatureLinkEditorService service,
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
        inspector = new CreatureLinkInspector(service);
    }

    private void ReleaseCapture()
    {
        if (lease.IsActive)
            lease.Release();
        lease = default;
    }

    public void Initialize()
    {
        renderStage = new CreatureLinkRenderStage(engine, service);
        overlays.SetSection(SpawnEditorTool.CreatureLink, inspector);
        // the render stage is registered on the first Update so it appends after the other modules'
        // stages and draws last (see FormationEditorModule/WaypointEditorModule).
    }

    public void Dispose()
    {
        ReleaseCapture();
        overlays.SetSection(SpawnEditorTool.CreatureLink, null);
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

        service.ToolEnabled = toolService.ActiveTool == SpawnEditorTool.CreatureLink;

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
            RemoveSelected();
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
            dragSlaveGuid = creature.Guid;
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
        if (PickLink(ray, out var picked))
        {
            service.Selected = picked;
            interaction.UsePointerThisFrame();
        }
        else
        {
            service.Selected = null;
        }
    }

    private void RemoveSelected()
    {
        switch (service.Selected)
        {
            case EditableCreatureLink guidLink:
                service.RemoveGuidLink(guidLink);
                break;
            case EditableCreatureLinkTemplate templateLink:
                service.RemoveTemplateLink(templateLink);
                break;
        }
    }

    private void UpdateDrag()
    {
        var target = PickCreatureUnderCursor();
        if (target != null && target.Guid != dragSlaveGuid)
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
            if (dragTargetGuid != 0 && dragTargetGuid != dragSlaveGuid)
                CommitDrag(dragSlaveGuid, dragTargetGuid);
            dragging = false;
            service.DragActive = false;
            interaction.UsePointerThisFrame();
            ReleaseCapture();
        }
    }

    /// <summary>Drop = link. Guid mode makes a creature_linking row; entry mode resolves both
    /// creatures' entries and makes a creature_linking_template row on the current map.</summary>
    private void CommitDrag(uint slaveGuid, uint masterGuid)
    {
        if (service.Mode == CreatureLinkMode.Entry && service.SupportsTemplateLinks)
        {
            if (service.TryGetCreatureEntry(slaveGuid, out var slaveEntry) &&
                service.TryGetCreatureEntry(masterGuid, out var masterEntry) &&
                service.LoadedMap >= 0)
                service.AddTemplateLink(slaveEntry, (uint)service.LoadedMap, masterEntry);
        }
        else
        {
            service.AddGuidLink(slaveGuid, masterGuid);
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

    /// <summary>Ray-vs-arrow pick over both link kinds. For entry links each fanned-out arrow is
    /// tested; a hit on any selects the whole template link.</summary>
    private bool PickLink(Ray ray, out object? picked)
    {
        picked = null;
        float bestDist = float.MaxValue;

        foreach (var link in service.GuidLinks)
        {
            if (!service.TryGetEndpoints(link, out var slavePos, out var masterPos))
                continue;
            if (TryHit(ray, slavePos, masterPos, ref bestDist))
                picked = link;
        }

        foreach (var link in service.TemplateLinks)
        {
            service.CollectTemplateArrows(link, arrowScratch);
            foreach (var (slavePos, masterPos) in arrowScratch)
            {
                if (TryHit(ray, slavePos, masterPos, ref bestDist))
                    picked = link;
            }
        }

        return picked != null;
    }

    private readonly List<(Vector3 slave, Vector3 master)> arrowScratch = new();

    private static bool TryHit(Ray ray, Vector3 a, Vector3 b, ref float bestDist)
    {
        var (dist, t, _) = ClosestRayToSegment(ray, a, b);
        if (t <= 0)
            return false;
        float threshold = PickAngularThreshold * t;
        if (dist < threshold && dist < bestDist)
        {
            bestDist = dist;
            return true;
        }
        return false;
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

    public void Render(float delta)
    {
    }

    public void RenderGUI()
    {
    }
}
