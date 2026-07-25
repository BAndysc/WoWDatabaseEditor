using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using TheMaths;

namespace TheEngine.Managers;

public abstract class BaseBaseView : IEngineView
{
    public bool IsVisible { get; private set; }
    public RectangleF ViewRect { get; private set; } = new RectangleF(0, 0, 1, 1);
    public float Aspect { get; private set; }
    public bool IsHovered { get; private set; }
    public bool HasFocus { get; private set; }

    // Overlay rects reported during GUI submission block world clicks the NEXT frame (game update
    // runs before GUI submission, so the current frame's rects can't be consulted yet). BeginWindow
    // runs once per frame before the game update - that's the swap point.
    private List<RectangleF> overlayRects = new();
    private List<RectangleF> prevOverlayRects = new();

    public void BlockClicksOver(RectangleF screenRect) => overlayRects.Add(screenRect);

    public bool IsPointerOverOverlay
    {
        get
        {
            if (prevOverlayRects.Count == 0)
                return false;
            var mouse = ImGui.GetMousePos();
            foreach (var rect in prevOverlayRects)
                if (rect.Contains(mouse.X, mouse.Y))
                    return true;
            return false;
        }
    }

    protected unsafe void BeginWindow(ReadOnlySpan<byte> title, IntPtr imageId, bool fullScreen)
    {
        (prevOverlayRects, overlayRects) = (overlayRects, prevOverlayRects);
        overlayRects.Clear();

        ImGuiWindowFlags flags = 0;
        if (fullScreen)
        {
            var viewport = ImGui.GetMainViewport();
            // flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
            // ImGui.SetNextWindowPos(new Vector2(0, 0));
            // ImGui.SetNextWindowSize(viewport.Size / ImGui.GetIO().DisplayFramebufferScale);
        }
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        IsVisible = ImGuiEx.Begin(title, flags);
        ImGui.PopStyleVar();
        var contentSize = ImGui.GetContentRegionAvail();
        ViewRect = new RectangleF(ImGui.GetCursorScreenPos().X, ImGui.GetCursorScreenPos().Y,
            Math.Max(1, contentSize.X), Math.Max(1, contentSize.Y));
        Aspect = contentSize.Y == 0 ? 1 : contentSize.X / contentSize.Y;
        IsHovered = ImGui.IsWindowHovered();
        HasFocus = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        // native top-down render targets: no V-flip (uv0 top-left, uv1 bottom-right)
        ImGui.Image(new ImTextureRef(null, imageId), contentSize, new Vector2(0, 0), new Vector2(1, 1));

        ImGuizmo.SetOrthographic(false);
        ImGuizmo.SetDrawlist();

        ImGuizmo.SetRect(
            ViewRect.X,
            ViewRect.Y,
            ViewRect.Width,
            ViewRect.Height
        );
    }

    protected void EndWindow()
    {
        ImGui.End();
    }
}