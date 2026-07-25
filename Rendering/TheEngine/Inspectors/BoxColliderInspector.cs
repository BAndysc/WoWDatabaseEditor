using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Physics;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class BoxColliderInspector : IRefInspectorDrawer<BoxCollider>
{
    public void Draw(Entity entity, ref BoxCollider component)
    {
        ImGui.Columns(2, "boxcollider", false);

        ImGuiEx.TextUnformatted("Size\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat3("##Size", ref component.Size, 0.05f, 0.001f, 10000f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Center\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat3("##Center", ref component.Center, 0.05f);
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
