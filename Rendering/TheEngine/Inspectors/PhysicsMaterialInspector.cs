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
        ImGui.Columns(2, "physicsmaterial", false);

        ImGuiEx.TextUnformatted("Friction\0"u8);
        ImGui.NextColumn();
        ImGui.DragFloat("##Friction", ref component.Friction, 0.02f, 0f, 10f);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Bounciness\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##Bounciness", ref component.Bounciness, 0, 1);
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
