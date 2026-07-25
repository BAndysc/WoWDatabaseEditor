using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Physics;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class SphereColliderInspector : IRefInspectorDrawer<SphereCollider>
{
    public void Draw(Entity entity, ref SphereCollider component)
    {
        ImGui.Columns(2, "spherecollider"u8, false);

        ImGuiEx.TextUnformatted("Radius\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat("##Radius"u8, ref component.Radius, 0.05f, 0.001f, 10000f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Center\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat3("##Center", ref component.Center, 0.05f);
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
