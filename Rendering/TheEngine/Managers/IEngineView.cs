using TheMaths;

namespace TheEngine.Managers;

public interface IEngineView
{
    bool IsVisible { get; }
    RectangleF ViewRect { get; }
    float Aspect { get; }
    bool IsHovered { get; }
    bool HasFocus { get; }

    /// <summary>Reports a screen-space rect occupied by interactive UI drawn inside this view (an
    /// in-view toolbar, an overlay button strip). World clicks are suppressed while the mouse is
    /// over any reported rect. Rects live for one frame - re-report every frame from GUI code.
    /// The suppression is one frame stale (GUI is submitted after the game update), which is fine
    /// for UI that doesn't move under the cursor.</summary>
    void BlockClicksOver(RectangleF screenRect);

    /// <summary>True when the mouse is over an overlay rect reported last frame.</summary>
    bool IsPointerOverOverlay { get; }
}