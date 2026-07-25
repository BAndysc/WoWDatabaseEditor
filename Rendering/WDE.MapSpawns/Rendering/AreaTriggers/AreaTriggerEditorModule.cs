using Hexa.NET.ImGui;
using TheEngine;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.AreaTriggers;
using WDE.MapSpawns.Rendering.WorldPoints;
using WDE.MpqReader.DBC;

namespace WDE.MapSpawns.Rendering.AreaTriggers;

/// <summary>
/// The Area-trigger tool: every AreaTrigger.dbc shape on the current map renders as a clickable
/// wireframe (the shapes themselves are client data - not editable); selecting one edits its
/// database side tables (teleport destination + requirements, tavern, exploration quest, script)
/// in the inspector. Teleport destinations targeting the current map are the movable beam markers
/// inherited from <see cref="WorldPointModuleBase"/>, with a portal arrow from the trigger.
/// </summary>
public class AreaTriggerEditorModule : WorldPointModuleBase
{
    private static readonly Vector4 DestinationColor = new(0.30f, 0.85f, 0.80f, 0.95f); // portal teal
    private static readonly Vector4 TriggerColor = new(0.35f, 0.60f, 1.00f, 0.85f);
    private static readonly Vector4 TriggerWithDataColor = new(0.30f, 0.85f, 0.80f, 0.9f);
    private static readonly Vector4 TriggerSelectedColor = new(1.00f, 0.85f, 0.30f, 1f);
    private const float TriggerMaxDistance = 900f;

    public struct TriggerShape
    {
        public uint Id;
        public Vector3 Position;
        public bool IsBox;
        public float Radius;
        public Vector3 BoxHalf;
        public float BoxYaw;
        /// <summary>Label anchor height (box half height / sphere radius).</summary>
        public float Height;
    }

    private readonly IAreaTriggerEditorService service;
    private readonly AreaTriggerInspector inspector;
    private bool loadInFlight;

    private readonly List<TriggerShape> triggers = new();
    private int triggersBuiltForMap = int.MinValue;

    // chip labels are cached - rebuilding strings every frame is per-frame garbage
    private readonly Dictionary<uint, string> triggerLabels = new();
    private int labelsBuiltForRevision = -1;
    private int labelsBuiltForMap = int.MinValue;

    public IAreaTriggerEditorService Service => service;
    public IReadOnlyList<TriggerShape> Triggers => triggers;

    public AreaTriggerEditorModule(Engine engine,
        IGameContext gameContext,
        ISpawnEditorToolService toolService,
        IWorldInteractionService interaction,
        IInputManager inputManager,
        IGameViewOverlayService overlays,
        RaycastSystem raycastSystem,
        IAreaTriggerEditorService service,
        EntryPickerService entryPicker)
        : base(engine, gameContext, toolService, interaction, inputManager, overlays, raycastSystem)
    {
        this.service = service;
        inspector = new AreaTriggerInspector(service, this, gameContext, entryPicker);
    }

    protected override SpawnEditorTool Tool => SpawnEditorTool.AreaTrigger;
    protected override int GizmoId => 4;
    protected override IInspectorSection Section => inspector;

    // global data, loaded once - unsaved edits survive map switches
    protected override void PumpAndSync()
    {
        service.PumpPendingLoads();
        SyncTriggers();
        SyncLabels();
        if (!service.IsSupported || loadInFlight || service.HasData)
            return;
        loadInFlight = true;
        Load().ListenErrors();
    }

    public void Reload()
    {
        if (loadInFlight)
            return;
        loadInFlight = true;
        Load().ListenErrors();
    }

    private async Task Load()
    {
        try
        {
            await service.LoadForMap(CurrentMapId);
        }
        finally
        {
            loadInFlight = false;
        }
    }

    private unsafe void SyncTriggers()
    {
        if (triggersBuiltForMap == CurrentMapId)
            return;
        triggersBuiltForMap = CurrentMapId;
        triggers.Clear();
        foreach (var areaTrigger in gameContext.DbcManager.AreaTriggerStore)
        {
            if (areaTrigger->ContinentId != CurrentMapId)
                continue;
            var shape = new TriggerShape
            {
                Id = (uint)areaTrigger->Id,
                Position = new Vector3(areaTrigger->X, areaTrigger->Y, areaTrigger->Z),
            };
            if (areaTrigger->Shape == AreaTriggerShape.Box)
            {
                shape.IsBox = true;
                shape.BoxHalf = new Vector3(areaTrigger->BoxLength / 2, areaTrigger->BoxWidth / 2, areaTrigger->BoxHeight / 2);
                shape.BoxYaw = areaTrigger->BoxYaw;
                shape.Height = areaTrigger->BoxHeight / 2;
            }
            else
            {
                shape.Radius = areaTrigger->Radius;
                shape.Height = areaTrigger->Radius;
            }
            triggers.Add(shape);
        }
        triggers.Sort((a, b) => a.Id.CompareTo(b.Id));
    }

    private void SyncLabels()
    {
        if (labelsBuiltForRevision == service.Revision && labelsBuiltForMap == CurrentMapId)
            return;
        labelsBuiltForRevision = service.Revision;
        labelsBuiltForMap = CurrentMapId;
        triggerLabels.Clear();
        foreach (var trigger in triggers)
            triggerLabels[trigger.Id] = BuildTriggerLabel(trigger.Id);
    }

    private string BuildTriggerLabel(uint id)
    {
        string label = service.Teleports.TryGetValue(id, out var teleport) && !string.IsNullOrEmpty(teleport.Name)
            ? $"AT {id} · {teleport.Name}"
            : $"AT {id}";
        if (service.Taverns.ContainsKey(id))
            label += " [tavern]";
        if (service.QuestRelations.ContainsKey(id))
            label += " [quest]";
        if (service.ScriptNames.ContainsKey(id))
            label += " [script]";
        return label;
    }

    public string DescribeTrigger(uint id) =>
        triggerLabels.TryGetValue(id, out var label) ? label : $"AT {id}";

    public bool TryGetTrigger(uint id, out TriggerShape shape)
    {
        foreach (var trigger in triggers)
        {
            if (trigger.Id == id)
            {
                shape = trigger;
                return true;
            }
        }
        shape = default;
        return false;
    }

    protected override int DataRevision => service.Revision;

    protected override void CollectPoints(List<WorldPoint> output)
    {
        var map = (uint)CurrentMapId;
        foreach (var row in service.Teleports.Values)
        {
            if (row.Map != map)
                continue;
            output.Add(new WorldPoint
            {
                Key = row.Id,
                Position = row.Position,
                Orientation = row.Orientation,
                Label = $"-> {(string.IsNullOrEmpty(row.Name) ? $"AT {row.Id}" : row.Name)}",
                Color = DestinationColor,
            });
        }
    }

    protected override bool SelectionStillExists(uint key)
    {
        // a trigger shape on this map is a valid selection even without any database rows
        foreach (var trigger in triggers)
        {
            if (trigger.Id == key)
                return true;
        }
        return base.SelectionStillExists(key);
    }

    protected override bool TryGetSelectedTransform(out Vector3 position, out float orientation)
    {
        position = default;
        orientation = 0;
        if (SelectedKey is not { } key || !service.Teleports.TryGetValue(key, out var row))
            return false;
        if (row.Map != (uint)CurrentMapId)
            return false; // the destination lives on another map - nothing to drag here
        position = row.Position;
        orientation = row.Orientation;
        return true;
    }

    protected override void SetSelectedTransform(Vector3 position, float orientation)
    {
        if (SelectedKey is not { } key || !service.Teleports.TryGetValue(key, out var row))
            return;
        row.Position = position;
        row.Orientation = orientation;
        service.NotifyTeleportChanged(key);
    }

    // armed by the inspector: the next terrain click puts the SELECTED trigger's destination there
    protected override void PlaceAt(Vector3 position)
    {
        if (SelectedKey is not { } key)
            return;
        if (service.Teleports.TryGetValue(key, out var row))
        {
            row.Map = (uint)CurrentMapId;
            row.Position = position;
            service.NotifyTeleportChanged(key);
        }
        else
        {
            service.CreateTeleport(key, (uint)CurrentMapId, position, 0f);
        }
    }

    protected override void DeleteSelected(uint key) => service.DeleteTeleport(key);

    public void CreateTeleportAtCamera(uint triggerId)
    {
        var position = SnapToGround(engine.CameraManager.MainCamera.Transform.Position);
        service.CreateTeleport(triggerId, (uint)CurrentMapId, position, 0f);
    }

    protected override bool TryPickExtra(Ray ray, out uint key)
    {
        key = 0;
        float bestT = float.MaxValue;
        foreach (var trigger in triggers)
        {
            if (!RayIntersectsTrigger(ray, in trigger, out var t) || t >= bestT)
                continue;
            bestT = t;
            key = trigger.Id;
        }
        return bestT < float.MaxValue;
    }

    private static bool RayIntersectsTrigger(Ray ray, in TriggerShape trigger, out float t)
    {
        t = 0;
        if (trigger.IsBox)
        {
            // world -> box local: translate to the center, un-rotate the yaw around Z
            var delta = ray.Position - trigger.Position;
            float cos = MathF.Cos(trigger.BoxYaw);
            float sin = MathF.Sin(trigger.BoxYaw);
            var localOrigin = new Vector3(delta.X * cos + delta.Y * sin, -delta.X * sin + delta.Y * cos, delta.Z);
            var d = ray.Direction;
            var localDir = new Vector3(d.X * cos + d.Y * sin, -d.X * sin + d.Y * cos, d.Z);
            return RayIntersectsCenteredBox(localOrigin, localDir, trigger.BoxHalf, out t);
        }

        var toCenter = trigger.Position - ray.Position;
        float proj = Vector3.Dot(toCenter, ray.Direction);
        if (proj < 0)
            return false;
        float distSq = toCenter.LengthSquared() - proj * proj;
        float radiusSq = trigger.Radius * trigger.Radius;
        if (distSq > radiusSq)
            return false;
        t = proj - MathF.Sqrt(radiusSq - distSq);
        if (t < 0)
            t = 0; // the camera is inside the sphere
        return true;
    }

    private static bool RayIntersectsCenteredBox(Vector3 origin, Vector3 dir, Vector3 half, out float t)
    {
        t = 0;
        float tMin = 0f;
        float tMax = float.MaxValue;
        for (int axis = 0; axis < 3; ++axis)
        {
            float o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            float d = axis == 0 ? dir.X : axis == 1 ? dir.Y : dir.Z;
            float h = axis == 0 ? half.X : axis == 1 ? half.Y : half.Z;
            if (MathF.Abs(d) < 1e-6f)
            {
                if (MathF.Abs(o) > h)
                    return false;
                continue;
            }
            float t1 = (-h - o) / d;
            float t2 = (h - o) / d;
            if (t1 > t2)
                (t1, t2) = (t2, t1);
            tMin = MathF.Max(tMin, t1);
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax)
                return false;
        }
        t = tMin;
        return true;
    }

    public override void Render(float delta)
    {
        base.Render(delta);
        if (toolService.ActiveTool != Tool)
            return;

        var rm = engine.RenderManager;
        var cameraPos = engine.CameraManager.MainCamera.Transform.Position;
        var currentMap = (uint)CurrentMapId;

        foreach (var trigger in triggers)
        {
            bool selected = SelectedKey == trigger.Id;
            if (!selected && (trigger.Position - cameraPos).LengthSquared() > TriggerMaxDistance * TriggerMaxDistance)
                continue;

            bool hasTeleport = service.Teleports.TryGetValue(trigger.Id, out var teleport);
            bool hasAnyData = hasTeleport || service.Taverns.ContainsKey(trigger.Id) ||
                              service.QuestRelations.ContainsKey(trigger.Id) ||
                              service.ScriptNames.ContainsKey(trigger.Id);
            var color = selected ? TriggerSelectedColor : hasAnyData ? TriggerWithDataColor : TriggerColor;

            if (trigger.IsBox)
                DrawBoxWireframe(rm, in trigger, color);
            else
                rm.DrawSphere(trigger.Position, trigger.Radius, color);

            // portal arrow to the destination when it is on this map
            if (hasTeleport && teleport!.Map == currentMap)
                DrawPortalArrow(rm, trigger.Position + Vectors.Up * (trigger.Height * 0.5f), teleport.Position + Vectors.Up * 0.4f, color);
        }
    }

    private static void DrawBoxWireframe(IRenderManager rm, in TriggerShape trigger, Vector4 color)
    {
        float cos = MathF.Cos(trigger.BoxYaw);
        float sin = MathF.Sin(trigger.BoxYaw);
        var half = trigger.BoxHalf;

        Span<Vector3> corners = stackalloc Vector3[8];
        for (int i = 0; i < 8; ++i)
        {
            float x = (i & 1) == 0 ? -half.X : half.X;
            float y = (i & 2) == 0 ? -half.Y : half.Y;
            float z = (i & 4) == 0 ? -half.Z : half.Z;
            corners[i] = trigger.Position + new Vector3(x * cos - y * sin, x * sin + y * cos, z);
        }

        // bottom, top, verticals
        rm.DrawLine(corners[0], corners[1], color);
        rm.DrawLine(corners[1], corners[3], color);
        rm.DrawLine(corners[3], corners[2], color);
        rm.DrawLine(corners[2], corners[0], color);
        rm.DrawLine(corners[4], corners[5], color);
        rm.DrawLine(corners[5], corners[7], color);
        rm.DrawLine(corners[7], corners[6], color);
        rm.DrawLine(corners[6], corners[4], color);
        rm.DrawLine(corners[0], corners[4], color);
        rm.DrawLine(corners[1], corners[5], color);
        rm.DrawLine(corners[2], corners[6], color);
        rm.DrawLine(corners[3], corners[7], color);
    }

    private static void DrawPortalArrow(IRenderManager rm, Vector3 from, Vector3 to, Vector4 color)
    {
        rm.DrawLine(from, to, color);

        var dir = to - from;
        float length = dir.Length();
        if (length < 0.01f)
            return;
        dir /= length;
        var side = Vector3.Cross(dir, Vectors.Up);
        if (side.LengthSquared() < 0.001f)
            side = Vector3.Cross(dir, Vectors.Right);
        side = Vector3.Normalize(side);
        float headSize = MathF.Min(1.5f, length * 0.2f);
        rm.DrawLine(to, to - dir * headSize + side * headSize * 0.5f, color);
        rm.DrawLine(to, to - dir * headSize - side * headSize * 0.5f, color);
    }

    public override void RenderGUI()
    {
        base.RenderGUI(); // destination marker chips + drag guides

        if (toolService.ActiveTool != Tool || triggers.Count == 0)
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
        var cameraPos = camera.Transform.Position;

        foreach (var trigger in triggers)
        {
            bool emphasized = SelectedKey == trigger.Id;
            if (!emphasized && (trigger.Position - cameraPos).LengthSquared() > TriggerMaxDistance * TriggerMaxDistance)
                continue;
            var top = trigger.Position + Vectors.Up * trigger.Height;
            var clip = Vector4.Transform(new Vector4(top.X, top.Y, top.Z, 1f), viewProj);
            if (clip.W <= 0)
                continue;
            float nx = (clip.X / clip.W + 1f) * 0.5f;
            float ny = 1f - (clip.Y / clip.W + 1f) * 0.5f;
            if (nx < 0 || nx > 1 || ny < 0 || ny > 1)
                continue;

            string label = DescribeTrigger(trigger.Id);
            var textSize = ImGui.CalcTextSize(label);
            var pad = new System.Numerics.Vector2(5, 2);
            var anchor = new System.Numerics.Vector2(view.X + nx * view.Width, view.Y + ny * view.Height);
            var min = anchor + new System.Numerics.Vector2(-textSize.X * 0.5f - pad.X, -textSize.Y - pad.Y * 2);
            var max = min + textSize + pad * 2;

            dl.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.WindowBg, emphasized ? 0.95f : 0.70f), 4f);
            if (emphasized)
                dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Text, 0.7f), 4f);
            dl.AddText(min + pad, ImGui.GetColorU32(ImGuiCol.Text, emphasized ? 1f : 0.85f), label);
        }

        ImGui.End();
    }
}
