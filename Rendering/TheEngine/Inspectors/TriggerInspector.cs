using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Physics;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class TriggerInspector : IRefInspectorDrawer<Trigger>
{
    public void Draw(Entity entity, ref Trigger component)
    {
        ImGuiEx.TextUnformatted("Sensor: reports overlap events, generates no contact response\0"u8);
    }
}
