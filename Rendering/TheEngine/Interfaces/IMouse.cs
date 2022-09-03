using TheEngine.Input;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IMouse
    {
        bool IsMouseDown(MouseButton button);
        bool HasJustClicked(MouseButton button);
        bool HasJustReleased(MouseButton button);
        Vector2 Delta { get; }
        Vector2 WheelDelta { get; }
        Vector2 NormalizedPosition { get; }
        Vector2 ScreenPoint { get; }
        uint ClickCount { get; }
        bool HasJustDoubleClicked { get; }
        Vector2 LastClickNormalizedPosition { get; }
        Vector2 LastClickScreenPosition { get; }
    }
}