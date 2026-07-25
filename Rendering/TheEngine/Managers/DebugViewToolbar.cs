using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using TheEngine.Interfaces;
using TheEngine.Rendering;

namespace TheEngine.Managers;

/// <summary>Overlay toolbars drawn on top of the game / scene views. The two views used to share a
/// single toolbar; they are deliberately kept as separate methods now so each can carry its own
/// controls (the scene view adds gizmo-mode buttons, the game view does not).</summary>
internal class DebugViewToolbar
{
    /// <summary>Scene-view toolbar: gizmo-mode buttons (Move / Rotate / Scale), the own-culling
    /// toggle, plus the debug-view combobox.</summary>
    public void DrawSceneToolbar(Engine engine)
    {
        DrawGizmoModeButtons(engine);
        DrawCullToggle(engine);
        DrawDebugViewCombo(engine.renderManager);
    }

    // Toggles whether the scene view culls with its own camera or mirrors the game view's set.
    private void DrawCullToggle(Engine engine)
    {
        var sceneView = engine.sceneView;
        float top = ImGui.GetCursorStartPos().Y;

        const string label = "Scene cull";
        float w = ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2;
        ImGui.SetCursorPos(new Vector2(ImGui.GetWindowWidth() - 160f - 12f - w - 8f, top + 6));

        bool own = sceneView.OwnCulling;
        if (own)
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
        if (ImGui.Button(label))
            sceneView.OwnCulling = !sceneView.OwnCulling;
        if (own)
            ImGui.PopStyleColor();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(own
                ? "Scene view culls with its own camera"
                : "Scene view shows what the game view renders");
    }

    // Color RGB-axis icons loaded once from icons/*.png and shown on the gizmo-mode buttons.
    private ITexture? translateIcon, rotateIcon, scaleIcon;
    private const float IconSize = 22f;

    private void EnsureIcons(Engine engine)
    {
        if (translateIcon != null)
            return;
        translateIcon = engine.textureManager.LoadTexture("icons/gizmo_translate.png");
        rotateIcon = engine.textureManager.LoadTexture("icons/gizmo_rotate.png");
        scaleIcon = engine.textureManager.LoadTexture("icons/gizmo_scale.png");
        // built after the loads - a field initializer would capture the icons while still null
        modes = new[]
        {
            (ImGuizmoOperation.Translate, translateIcon, "Move (translate)"),
            (ImGuizmoOperation.Rotate, rotateIcon, "Rotate"),
            (ImGuizmoOperation.Scale, scaleIcon, "Scale"),
        };
    }

    private (ImGuizmoOperation op, ITexture? icon, string tooltip)[] modes = System.Array.Empty<(ImGuizmoOperation, ITexture?, string)>();
    
    // Centered Move/Rotate/Scale icon buttons that pick which transform the scene-view gizmo manipulates.
    private unsafe void DrawGizmoModeButtons(Engine engine)
    {
        EnsureIcons(engine);
        var inspector = engine.EntityInspector;

        float top = ImGui.GetCursorStartPos().Y;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var pad = ImGui.GetStyle().FramePadding;


        float buttonWidth = IconSize + pad.X * 2;
        float totalWidth = buttonWidth * modes.Length + spacing * (modes.Length - 1);
        ImGui.SetCursorPos(new Vector2((ImGui.GetWindowWidth() - totalWidth) * 0.5f, top + 6));

        for (int i = 0; i < modes.Length; i++)
        {
            var (op, icon, tooltip) = modes[i];
            if (i > 0)
                ImGui.SameLine();
            if (icon == null)
                continue;

            bool active = inspector.GizmoOperation == op;
            ImGui.PushStyleColor(ImGuiCol.Button, active ? ImGui.GetColorU32(ImGuiCol.ButtonActive) : 0u);
            var tint = active ? Vector4.One : new Vector4(1, 1, 1, 0.7f);
            var texRef = new ImTextureRef(null, icon.Handle.ToRawIntPtr());
            if (ImGui.ImageButton($"##gizmo{i}", texRef, new Vector2(IconSize, IconSize),
                    new Vector2(0, 0), new Vector2(1, 1), new Vector4(0, 0, 0, 0), tint))
                inspector.GizmoOperation = op;
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }
    }

    private void DrawDebugViewCombo(RenderManager rm)
    {
        const float width = 160f;
        // GetCursorStartPos().Y is the content-region top (below the docking tab bar); SetCursorPos's
        // Y is measured from the window top, so without this the combo hides behind the tab bar.
        float top = ImGui.GetCursorStartPos().Y;
        ImGui.SetCursorPos(new Vector2(ImGui.GetWindowWidth() - width - 12, top + 6));
        ImGui.SetNextItemWidth(width);
        if (ImGuiEx.BeginCombo("##debugview\0"u8, Label(rm.DebugView), ImGuiComboFlags.None))
        {
            Item(rm, DebugView.FinalImage, "Final image\0"u8);
            Item(rm, DebugView.Depth, "Depth\0"u8);
            Item(rm, DebugView.ShadowCascade, "Shadow map\0"u8);
            Item(rm, DebugView.LightComplexity, "Light complexity\0"u8);
            Item(rm, DebugView.DecalComplexity, "Decal complexity\0"u8);
            ImGui.EndCombo();
        }
    }

    private void Item(RenderManager rm, DebugView view, ReadOnlySpan<byte> label)
    {
        if (ImGuiEx.Selectable(label, rm.DebugView == view))
            rm.DebugView = view;
        if (rm.DebugView == view)
            ImGui.SetItemDefaultFocus();
    }

    private ReadOnlySpan<byte> Label(DebugView view) => view switch
    {
        DebugView.Depth => "Depth\0"u8,
        DebugView.ShadowCascade => "Shadow map\0"u8,
        DebugView.LightComplexity => "Light complexity\0"u8,
        DebugView.DecalComplexity => "Decal complexity\0"u8,
        _ => "Final image\0"u8,
    };
}
