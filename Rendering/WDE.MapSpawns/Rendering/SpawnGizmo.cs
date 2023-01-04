using Prism.Ioc;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.Utils;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering;

public class SpawnDragger : System.IDisposable
{
    private readonly ISpawnSelectionService spawnSelectionService;
    private SpawnGizmo gizmo;
    
    public SpawnDragger(IContainerProvider containerProvider,
        ISpawnSelectionService spawnSelectionService)
    {
        this.spawnSelectionService = spawnSelectionService;
        gizmo = containerProvider.Resolve<SpawnGizmo>((typeof(uint), Collisions.COLLISION_MASK_STATIC));
    }

    public bool Update(float delta)
    {
        if (spawnSelectionService.SelectedSpawn.Value is { } selectedSpawn && selectedSpawn.IsSpawned)
        {
            gizmo.IsEnabled = true;
            gizmo.GizmoPosition = selectedSpawn.WorldObject!.Position;
            gizmo.CanRotate = true;
            gizmo.RotationLock = selectedSpawn is CreatureSpawnInstance
                ? RotationLockType.RotationZ
                : RotationLockType.None;
        }
        else
            gizmo.IsEnabled = false;

        return gizmo.Update(delta);
    }

    public void RenderTransparent()
    {
         gizmo.Render();
    }

    public void Dispose()
    {
        gizmo.Dispose();
    }
}

public class SpawnGizmo : Dragger<SpawnInstance>
{
    private readonly ISpawnSelectionService spawnSelectionService;

    public SpawnGizmo(IMeshManager meshManager,
        IMaterialManager materialManager, 
        ICameraManager cameraManager, 
        IRenderManager renderManager, 
        RaycastSystem raycastSystem, 
        IInputManager inputManager,
        ISpawnSelectionService spawnSelectionService,
        uint collisionMask) : base(meshManager, materialManager, cameraManager, renderManager, raycastSystem, inputManager, collisionMask)
    {
        this.spawnSelectionService = spawnSelectionService;
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
        // todo: present the change to the user as sql and ask if you want to apply it to the database
        foreach (var o in objects)
        {
            if (o is CreatureSpawnInstance cr && cr.IsSpawned)
                Console.WriteLine("Creature rotated to " + cr.Creature!.Orientation);
            else if (o is GameObjectSpawnInstance g && g.IsSpawned)
                Console.WriteLine("Object rotation " + g.GameObject!.Rotation);
        }
    }

    protected override void FinishDragging(IEnumerable<SpawnInstance> objects)
    {
        // todo: present the change to the user as sql and ask if you want to apply it to the database
        foreach (var o in objects)
        {
            Console.WriteLine($"Object moved to ({o.Position})");
        }
    }
}