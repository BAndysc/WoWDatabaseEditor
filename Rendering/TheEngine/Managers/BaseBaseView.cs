using ImGuiNET;
using TheMaths;

namespace TheEngine.Managers;

public abstract class BaseBaseView : IEngineView
{
    public bool IsVisible { get; private set; }
    public RectangleF ViewRect { get; private set; } = new RectangleF(0, 0, 1, 1);
    public float Aspect { get; private set; }
    public bool IsHovered { get; private set; }
    public bool HasFocus { get; private set; }

    protected void BeginWindow(ReadOnlySpan<byte> title, IntPtr imageId, bool fullScreen)
    {
        ImGuiWindowFlags flags = 0;
        if (fullScreen)
        {
            var viewport = ImGui.GetMainViewport();
            // flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
            // ImGui.SetNextWindowPos(new Vector2(0, 0));
            // ImGui.SetNextWindowSize(viewport.Size / ImGui.GetIO().DisplayFramebufferScale);
        }
        IsVisible = ImGuiEx.Begin(title, flags);
        var contentSize = ImGui.GetContentRegionAvail();
        ViewRect = new RectangleF(ImGui.GetCursorScreenPos().X, ImGui.GetCursorScreenPos().Y,
            Math.Max(1, contentSize.X), Math.Max(1, contentSize.Y));
        Aspect = contentSize.Y == 0 ? 1 : contentSize.X / contentSize.Y;
        IsHovered = ImGui.IsWindowHovered();
        HasFocus = ImGui.IsWindowFocused();
        ImGui.Image(imageId, contentSize, new Vector2(0, 1), new Vector2(1, 0));
    }

    protected void EndWindow()
    {
        ImGui.End();
    }
}