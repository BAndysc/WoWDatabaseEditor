using ImGuiNET;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class CopyParentTransformInspector : IRefInspectorDrawer<CopyParentTransform>
{
    private readonly Engine engine;

    public CopyParentTransformInspector(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(Entity entity, ref CopyParentTransform component)
    {
        if (ImGui.Button($"Parent: {component.Parent}"))
        {
            engine.EntityInspector.InspectEntity(component.Parent);
        }
        var local = component.Local ?? Matrix.Identity;
        if (InspectorUtils.EditTRS(ref local))
        {
            component.Local = local;
            if (engine.entityManager.HasComponent<DirtyPosition>(entity))
                engine.entityManager.GetComponent<DirtyPosition>(entity).Enable();
        }
    }
}