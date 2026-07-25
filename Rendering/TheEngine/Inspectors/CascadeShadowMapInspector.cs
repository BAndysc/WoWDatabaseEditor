using System.Numerics;
using Hexa.NET.ImGui;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Utils;

namespace TheEngine.Inspectors;

/// <summary>
/// Inspector for the directional shadow configuration. Cascade boundaries are edited as ascending
/// PERCENTAGES of the max shadow distance (like Unity's "Percent" working unit) with each slider
/// clamped to its neighbours, so it is impossible to author a non-monotonic or out-of-range split -
/// the absolute Split0..3 stored on the component always stay sorted within (0, MaxDistance].
/// </summary>
public class CascadeShadowMapInspector : IRefInspectorDrawer<CascadeShadowMap>
{
    private readonly Engine engine;

    public CascadeShadowMapInspector(Engine engine)
    {
        this.engine = engine;
    }

    // distinct per-cascade colours for the preview bar (blue / green / olive / red), like the Unity panel
    private static readonly Vector4[] CascadeColors =
    {
        new(0.30f, 0.52f, 0.90f, 1f),
        new(0.42f, 0.78f, 0.42f, 1f),
        new(0.80f, 0.78f, 0.34f, 1f),
        new(0.85f, 0.42f, 0.36f, 1f),
    };

    public void Draw(Entity entity, ref CascadeShadowMap component)
    {
        // max distance is the source of truth for the absolute scale; the three inner boundaries are
        // kept as fractions of it so editing the distance rescales the cascades instead of breaking
        // their ordering.
        float maxDistance = MathF.Max(component.Split3, 1f);
        float p1 = component.Split0 / maxDistance * 100f;
        float p2 = component.Split1 / maxDistance * 100f;
        float p3 = component.Split2 / maxDistance * 100f;
        // sanitise once so a freshly added / zeroed component shows sensible spread instead of 0/0/0
        if (!(p1 < p2 && p2 < p3 && p3 < 100f))
        {
            p1 = 5f; p2 = 17f; p3 = 50f;
        }

        bool changed = false;

        ImGui.Columns(2, "cascadeShadowMap", false);

        Label("Is Enabled\0"u8);
        bool enabled = !component.Disabled;
        if (ImGui.Checkbox("##enabled", ref enabled))
            component.Disabled = !enabled;
        ImGui.NextColumn();

        Label("Resolution\0"u8);
        ReadOnlySpan<byte> currentRes = component.Resolution switch
        {
            <= 1024 => "1024\0"u8,
            <= 2048 => "2048\0"u8,
            _ => "4096\0"u8,
        };
        if (ImGuiEx.BeginCombo("##resolution\0"u8, currentRes, ImGuiComboFlags.WidthFitPreview))
        {
            if (ImGuiEx.Selectable("1024\0"u8, component.Resolution == 1024))
                component.Resolution = 1024;
            if (ImGuiEx.Selectable("2048\0"u8, component.Resolution == 2048))
                component.Resolution = 2048;
            if (ImGuiEx.Selectable("4096\0"u8, component.Resolution == 4096))
                component.Resolution = 4096;
            ImGui.EndCombo();
        }
        ImGui.NextColumn();

        Label("Max Distance\0"u8);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.DragFloat("##maxDistance", ref maxDistance, 1f, 1f, 5000f))
            changed = true;
        ImGui.NextColumn();

        // each inner split is clamped to (previous, next) so they can never cross or leave [0,100]
        Label("Split 1 (%)\0"u8);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat("##split1", ref p1, 0.1f, p2 - 0.1f))
            changed = true;
        ImGui.NextColumn();

        Label("Split 2 (%)\0"u8);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat("##split2", ref p2, p1 + 0.1f, p3 - 0.1f))
            changed = true;
        ImGui.NextColumn();

        Label("Split 3 (%)\0"u8);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat("##split3", ref p3, p2 + 0.1f, 99.9f))
            changed = true;
        ImGui.NextColumn();

        Label("Caster Extrusion\0"u8);
        ImGui.SetNextItemWidth(-1);
        if (ImGui.SliderFloat("##casterExtrusion", ref component.CasterExtrusion, 0f, 500f))
            changed = true;
        ImGui.NextColumn();

        Label("Depth Bias (const)\0"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat("##depthBiasConstant", ref component.DepthBiasConstant, 0f, 8f);
        ImGui.NextColumn();

        Label("Depth Bias (slope)\0"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat("##depthBiasSlope", ref component.DepthBiasSlope, 0f, 8f);
        ImGui.NextColumn();

        Label("Normal Bias\0"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat("##normalBias", ref component.NormalBias, 0f, 0.5f);
        ImGui.NextColumn();

        Label("Constant Bias\0"u8);
        ImGui.SetNextItemWidth(-1);
        // world units along the light (converted per cascade in the shader)
        ImGui.SliderFloat("##constantBias", ref component.ConstantBias, 0f, 1f);
        ImGui.NextColumn();

        // PCF blur: radius is the kernel half-size (0 = hard shadows), blur the per-tap spacing.
        Label("PCF Radius\0"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderInt("##pcfRadius", ref component.PcfRadius, 0, 4);
        ImGui.NextColumn();

        Label("Blur\0"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat("##blur", ref component.Blur, 0f, 4f);
        ImGui.NextColumn();

        Label("Cascade Blend\0"u8);
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat("##cascadeBlend", ref component.CascadeBlend, 0f, 0.5f);
        ImGui.NextColumn();

        ImGui.Columns(1);

        // keep the percentages strictly ascending (the slider bounds above already enforce this, this
        // is just belt-and-suspenders for the initial frame) and write the absolute splits back
        p2 = MathF.Max(p2, p1 + 0.1f);
        p3 = MathF.Max(p3, p2 + 0.1f);
        if (changed)
        {
            component.Split3 = maxDistance;
            component.Split0 = p1 / 100f * maxDistance;
            component.Split1 = p2 / 100f * maxDistance;
            component.Split2 = p3 / 100f * maxDistance;
        }

        DrawCascadeBar(p1, p2, p3, maxDistance);
    }

    private static void Label(ReadOnlySpan<byte> text)
    {
        ImGuiEx.TextUnformatted(text);
        ImGui.NextColumn();
    }

    /// <summary>Colored 4-segment bar showing the relative cascade coverage (and the absolute end
    /// distance of each cascade), so the split distribution is visible at a glance.</summary>
    private static void DrawCascadeBar(float p1, float p2, float p3, float maxDistance)
    {
        ImGui.Spacing();
        Span<float> bounds = stackalloc float[5] { 0f, p1, p2, p3, 100f };

        var drawList = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        float width = MathF.Max(ImGui.GetContentRegionAvail().X, 1f);
        const float height = 20f;
        ImGui.Dummy(new Vector2(width, height));

        for (int i = 0; i < 4; i++)
        {
            float x0 = origin.X + width * bounds[i] / 100f;
            float x1 = origin.X + width * bounds[i + 1] / 100f;
            uint col = ImGui.ColorConvertFloat4ToU32(CascadeColors[i]);
            drawList.AddRectFilled(new Vector2(x0, origin.Y), new Vector2(x1, origin.Y + height), col);
        }
        // thin frame around the whole bar
        drawList.AddRect(origin, new Vector2(origin.X + width, origin.Y + height), ImGui.GetColorU32(ImGuiCol.Border));

        // per-cascade end distances, e.g. "0:13m  1:50m  2:150m  3:300m"
        ImGui.Text($"0:{bounds[1] / 100f * maxDistance:0.#}  " +
                   $"1:{bounds[2] / 100f * maxDistance:0.#}  " +
                   $"2:{bounds[3] / 100f * maxDistance:0.#}  " +
                   $"3:{maxDistance:0.#}");
    }
}
