using TheEngine.Interfaces;
using ImGuiNET;
using TheEngine;

namespace WDE.MapRenderer;

public class MainUi
{
    private readonly IRenderManager renderManager;
    private readonly IUIManager uiManager;

    public MainUi(IRenderManager renderManager,
        IUIManager uiManager)
    {
        this.renderManager = renderManager;
        this.uiManager = uiManager;
        uiManager.OnMenuBarDraw += DrawMenuBar;
    }

    private void DrawMenuBar()
    {
        if (ImGui.BeginMenu("Rendering"))
        {
            foreach (var layer in renderManager.RenderLayers)
            {
                if (!layer.IsUsed)
                    continue;
                bool enabled = !layer.IsDisabled;
                if (ImGuiEx.MenuItem(layer.Name, null, ref enabled))
                {
                    renderManager.ToggleRenderLayer(layer.Layer, enabled);
                }
            }
            ImGui.EndMenu();
        }
    }
}