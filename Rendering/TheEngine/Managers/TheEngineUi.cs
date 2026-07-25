using Hexa.NET.ImGui;
using TheEngine.Windows;

namespace TheEngine.Managers;

public class TheEngineUi
{
    private readonly Engine engine;
    private readonly EngineGameView gameView;
    private readonly EngineSceneView sceneView;

    private InputDebugWindow inputWindow;

    public TheEngineUi(Engine engine)
    {
        this.engine = engine;
        this.gameView = engine.gameView;
        this.sceneView = engine.sceneView;
        inputWindow = new InputDebugWindow(engine);
        engine.uiManager.OnMenuBarDraw += OnDrawMenuBar;
    }

    private void OnDrawMenuBar()
    {
        if (ImGuiEx.BeginMenu("Debug\0"u8))
        {
            bool isOpen = inputWindow.IsOpen;
            if (ImGuiEx.MenuItem("Input debugger\0"u8, null, ref isOpen))
            {
                inputWindow.IsOpen = !inputWindow.IsOpen;
            }

            ImGui.EndMenu();
        }
    }

    public void BeginFrame(float delta)
    {
#if !ENGINE_RELEASE
        sceneView.Draw(delta);

        // Default to the "3D" tab. gameView and sceneView are docked together as tabs and
        // only the active tab renders; the saved imgui.ini restores Scene View as selected,
        // which a SetNextWindowFocus before Begin can't override. Focusing "3D" by name here
        // (after both windows have been submitted this frame) is the last word and reliably
        // selects it. We stop once 3D has been the active tab once, so the user can switch
        // freely afterwards and their choice persists.
        if (!gameView.WasEverVisible)
            ImGui.SetWindowFocus("3D"u8);
#endif
        gameView.Draw(delta);
        inputWindow.Update(delta);
    }
}