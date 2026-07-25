using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class LocalToWorldInspector : IRefInspectorDrawer<LocalToWorld>
{
    private readonly Engine engine;

    public LocalToWorldInspector(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(Entity entity, ref LocalToWorld component)
    {
        var matrix = component.Matrix;
        if (InspectorUtils.EditTRS(ref matrix))
        {
            var newLocalToWorld = new LocalToWorld() { Matrix = matrix };
            component = newLocalToWorld;
            if (engine.entityManager.HasComponent<DirtyPosition>(entity))
                engine.entityManager.GetComponent<DirtyPosition>(entity).Enable();
            engine.physicsManager.SyncDynamicPoseFromEditor(entity, newLocalToWorld.Position, newLocalToWorld.Rotation);
        }
    }
}