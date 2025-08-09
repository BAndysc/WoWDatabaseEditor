using ImGuiNET;
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
        gameView.Draw(delta);
#if !ENGINE_RELEASE
        sceneView.Draw(delta);
#endif
        inputWindow.Update(delta);
    }
}