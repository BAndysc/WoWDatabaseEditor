using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Physics;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class RigidBodyInspector : IRefInspectorDrawer<RigidBody>
{
    public void Draw(Entity entity, ref RigidBody component)
    {
        ImGui.Columns(2, "rigidbody", false);

        ImGuiEx.TextUnformatted("Type\0"u8);
        ImGui.NextColumn();
        ReadOnlySpan<byte> currentTypeName = component.Type == BodyType.Dynamic ? "Dynamic\0"u8 : "Kinematic\0"u8;
        if (ImGuiEx.BeginCombo("##type\0"u8, currentTypeName, ImGuiComboFlags.WidthFitPreview))
        {
            if (ImGuiEx.Selectable("Dynamic\0"u8, component.Type == BodyType.Dynamic))
                component.Type = BodyType.Dynamic;
            if (ImGuiEx.Selectable("Kinematic\0"u8, component.Type == BodyType.Kinematic))
                component.Type = BodyType.Kinematic;
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Mass\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat("##Mass", ref component.Mass, 0.05f, 0.001f, 10000f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Linear Damping\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##LinearDamping", ref component.LinearDamping, 0, 1);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Angular Damping\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##AngularDamping", ref component.AngularDamping, 0, 1);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Gravity Scale\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat("##GravityScale", ref component.GravityScale, 0.05f, -10f, 10f);
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
