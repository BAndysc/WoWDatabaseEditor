using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.MapRenderer.Utils;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawnsEditor.Rendering;

public class SpawnGizmo : Dragger<SpawnInstance>
{
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IPendingGameChangesService pendingGameChangesService;

    public SpawnGizmo(IMeshManager meshManager,
        IMaterialManager materialManager, 
        ICameraManager cameraManager, 
        IRenderManager renderManager, 
        RaycastSystem raycastSystem, 
        IInputManager inputManager,
        ISpawnSelectionService spawnSelectionService,
        IPendingGameChangesService pendingGameChangesService,
        uint collisionMask) : base(meshManager, materialManager, cameraManager, renderManager, raycastSystem, inputManager, collisionMask)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.pendingGameChangesService = pendingGameChangesService;
    }

    public override Quaternion GetRotation(SpawnInstance item)
    {
        if (!item.IsSpawned)
            return Quaternion.Identity;
        
        if (item is CreatureSpawnInstance cr)
            return Quaternion.CreateFromAxisAngle(Vectors.Up, cr.Creature!.Orientation);
        else if (item is GameObjectSpawnInstance go)
            return go.GameObject!.Rotation;
        
        return Quaternion.Identity;
    }

    public override Vector3 GetPosition(SpawnInstance item)
    {
        if (!item.IsSpawned)
            return Vector3.Zero;
        
        if (item is CreatureSpawnInstance cr)
            return cr.Creature!.Position;
        else if (item is GameObjectSpawnInstance go)
            return go.GameObject!.Position;

        return Vector3.Zero;
    }

    protected override void Move(SpawnInstance item, Vector3 position)
    {
        if (!item.IsSpawned)
            return;

        item.WorldObject!.Position = position;
    }

    protected override void Rotate(SpawnInstance item, Quaternion rotation)
    {
        if (!item.IsSpawned)
            return;

        if (item is CreatureSpawnInstance cr)
            cr.Creature!.Orientation = rotation.Angle() * rotation.Axis().Z;
        else if (item is GameObjectSpawnInstance go)
            go.GameObject!.Rotation = rotation;
    }

    protected override IReadOnlyList<SpawnInstance>? PointsToDrag()
    {
        if (spawnSelectionService.SelectedSpawn.Value == null)
            return null;

        return new List<SpawnInstance>() { spawnSelectionService.SelectedSpawn.Value };
    }

    protected override void FinishRotation(IEnumerable<SpawnInstance> objects)
    {
        foreach (var o in objects)
        {
            if (!o.IsSpawned)
                continue;

            if (o.Guid == 0) // a phantom object
                continue;
            
            if (o is CreatureSpawnInstance cr && cr.IsSpawned)
            {
                pendingGameChangesService.AddQuery(GuidType.Creature, o.Entry, o.Guid, Queries.Table(DatabaseTable.WorldTable("creature"))
                    .Where(row => row.Column<uint>("guid") == o.Guid)
                    .Set("orientation", cr.Creature!.Orientation)
                    .Update());
            }
            else if (o is GameObjectSpawnInstance g && g.IsSpawned)
            {
                var rot = g.GameObject!.Rotation;
                pendingGameChangesService.AddQuery(GuidType.GameObject, o.Entry, o.Guid, Queries.Table(DatabaseTable.WorldTable("gameobject"))
                    .Where(row => row.Column<uint>("guid") == o.Guid)
                    .Set("rotation0", rot.X)
                    .Set("rotation1", rot.Y)
                    .Set("rotation2", rot.Z)
                    .Set("rotation3", rot.W)
                    .Update());
            }
        }
    }

    protected override void FinishDragging(IEnumerable<SpawnInstance> objects)
    {
        foreach (var o in objects)
        {
            if (!o.IsSpawned)
                continue;
            
            if (o.Guid == 0) // a phantom object
                continue;
            
            var guidType = o is CreatureSpawnInstance ? GuidType.Creature : GuidType.GameObject;
            pendingGameChangesService.AddQuery(guidType, o.Entry, o.Guid, Queries.Table(guidType == GuidType.Creature ? DatabaseTable.WorldTable("creature") : DatabaseTable.WorldTable("gameobject"))
                .Where(row => row.Column<uint>("guid") == o.Guid)
                .Set("position_x", o.WorldObject!.Position.X)
                .Set("position_y", o.WorldObject.Position.Y)
                .Set("position_z", o.WorldObject.Position.Z)
                .Update());
        }
    }
}