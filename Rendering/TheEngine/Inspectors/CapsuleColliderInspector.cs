using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Physics;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class CapsuleColliderInspector : IRefInspectorDrawer<CapsuleCollider>
{
    public void Draw(Entity entity, ref CapsuleCollider component)
    {
        ImGui.Columns(2, "capsulecollider", false);

        ImGuiEx.TextUnformatted("Radius\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat("##Radius", ref component.Radius, 0.05f, 0.001f, 10000f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Length\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat("##Length", ref component.Length, 0.05f, 0.001f, 10000f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Center\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat3("##Center", ref component.Center, 0.05f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("\0"u8);
        ImGui.NextColumn();
        ImGuiEx.TextUnformatted("Aligned along local Y axis\0"u8);
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
