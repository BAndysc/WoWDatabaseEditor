using Avalonia;
using TheMaths;

namespace TheEngine.Input
{
    public interface IMouse
    {
        bool IsMouseDown(MouseButton button);
        bool HasJustClicked(MouseButton button);
        Vector2 Delta { get; }
        short WheelDelta { get; }
        Vector2 NormalizedPosition { get; }
        Vector2 ScreenPoint { get; }
    }
}