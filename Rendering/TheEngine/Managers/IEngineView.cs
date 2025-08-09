using TheMaths;

namespace TheEngine.Managers;

public interface IEngineView
{
    bool IsVisible { get; }
    RectangleF ViewRect { get; }
    float Aspect { get; }
    bool IsHovered { get; }
    bool HasFocus { get; }
}