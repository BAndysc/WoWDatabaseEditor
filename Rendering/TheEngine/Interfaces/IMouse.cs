using TheEngine.Input;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface IMouse
    {
        bool IsMouseDown(MouseButton button);
        /// <summary>Button state without the view-hover gate - for finishing an interaction whose
        /// release may happen outside the game view (drag-commits).</summary>
        bool RawIsMouseDown(MouseButton button);
        bool HasJustClicked(MouseButton button);
        bool HasJustReleased(MouseButton button);
        Vector2 Delta { get; }
        Vector2 WheelDelta { get; }
        /// <summary>Wheel delta only while the pointer is over the 3D world itself (view hovered,
        /// not over in-view overlay chrome) - what camera zoom/fly-speed should read.</summary>
        Vector2 ViewWheelDelta { get; }
        Vector2 NormalizedPosition { get; }
        Vector2 ScreenPoint { get; }
        uint ClickCount { get; }
        bool HasJustDoubleClicked { get; }
        Vector2 LastClickNormalizedPosition { get; }
        Vector2 LastClickScreenPosition { get; }
    }
}