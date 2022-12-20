using Prism.Ioc;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.Utils;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.ViewModels;

namespace WDE.MapSpawnsEditor.Rendering;

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

    public void BeginDrag()
    {
        gizmo.StartSnapDrag();
    }

    public void FinishDrag()
    {
        gizmo.Finish();
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