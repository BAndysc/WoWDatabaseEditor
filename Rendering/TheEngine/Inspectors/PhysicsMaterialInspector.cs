using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Physics;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class PhysicsMaterialInspector : IRefInspectorDrawer<PhysicsMaterial>
{
    public void Draw(Entity entity, ref PhysicsMaterial component)
    {
        ImGui.Columns(2, "physicsmaterial"u8, false);

        ImGuiEx.TextUnformatted("Friction\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat("##Friction"u8, ref component.Friction, 0.02f, 0f, 10f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Bounciness\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##Bounciness"u8, ref component.Bounciness, 0, 1);
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
