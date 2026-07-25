using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

public class LightInspector : IRefInspectorDrawer<Light>
{
    private readonly Engine engine;

    public LightInspector(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(Entity entity, ref Light component)
    {
        ImGui.Columns(2, "light"u8, false);

        ImGuiEx.TextUnformatted("Is Enabled\0"u8);
        ImGui.NextColumn();
        bool enabled = !component.Disabled;
        if (ImGui.Checkbox("##enabled"u8, ref enabled))
            component.Disabled = !enabled;
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Type\0"u8);
        ImGui.NextColumn();
        ReadOnlySpan<byte> currentTypeName = component.Type == LightType.Directional ? "Directional\0"u8 : "Point\0"u8;
        if (ImGuiEx.BeginCombo("##type\0"u8, currentTypeName, ImGuiComboFlags.WidthFitPreview))
        {
            if (ImGuiEx.Selectable("Directional\0"u8, component.Type == LightType.Directional))
                component.Type = LightType.Directional;
            if (ImGuiEx.Selectable("Point\0"u8, component.Type == LightType.Point))
                component.Type = LightType.Point;
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Color\0"u8);
        ImGui.NextColumn();
        ImGui.ColorEdit4("##Color", ref component.Color);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Intensity\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##Intensity"u8, ref component.Intensity, 0, component.Type == LightType.Directional ? 5 : 2);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Cast Shadows\0"u8);
        ImGui.NextColumn();
        ImGui.Checkbox("##CastShadows"u8, ref component.CastShadows);
        ImGui.NextColumn();

        if (component.Type == LightType.Point)
        {
            ImGuiEx.TextUnformatted("Attenuation Start\0"u8);
            ImGui.NextColumn();
            ImGui.SliderFloat("##AttenuationStart"u8, ref component.AttenuationStart, 0, 1000);
            ImGui.NextColumn();

            ImGuiEx.TextUnformatted("Attenuation End\0"u8);
            ImGui.NextColumn();
            ImGui.SliderFloat("##AttenuationEnd"u8, ref component.AttenuationEnd, 0, 1000);
            ImGui.NextColumn();
        }
        else // directional
        {
            ImGuiEx.TextUnformatted("Ambient Color\0"u8);
            ImGui.NextColumn();
            ImGui.ColorEdit4("##AmbientColor", ref component.AmbientColor);
            ImGui.NextColumn();

            ImGuiEx.TextUnformatted("Direction\0"u8);
            ImGui.NextColumn();
            ImGuiEx.TextUnformatted("(from entity rotation)\0"u8);
            ImGui.NextColumn();
        }

        ImGui.Columns(1);
    }
}
