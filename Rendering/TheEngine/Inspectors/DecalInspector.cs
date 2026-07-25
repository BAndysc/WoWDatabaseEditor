using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Utils;
using TheEngine.Windows;

namespace TheEngine.Inspectors;

public class DecalInspector : IRefInspectorDrawer<Decal>
{
    private readonly Engine engine;

    public DecalInspector(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(Entity entity, ref Decal component)
    {
        ImGui.Columns(2, "decal", false);

        ImGuiEx.TextUnformatted("Is Enabled\0"u8);
        ImGui.NextColumn();
        bool enabled = !component.Disabled;
        if (ImGui.Checkbox("##enabled", ref enabled))
        {
            component.Disabled = !enabled;
        }
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Color\0"u8);
        ImGui.NextColumn();
        ImGui.ColorEdit4("##Color", ref component.Color);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Fade Angle\0"u8);
        ImGui.NextColumn();
        ImGui.SliderFloat("##FadeAngleCos", ref component.FadeAngleCos, 0, 1);
        ImGui.NextColumn();

        ImGuiEx.TextUnformatted("Albedo\0"u8);
        ImGui.NextColumn();
        var albedo = component.Albedo;
        if (TexturePickerWindow.Field(engine, $"decal_albedo_{entity.Id}", ref albedo))
        {
            component.Albedo = albedo;
        }
        ImGui.NextColumn();

        ImGui.Columns(1);
    }
}
