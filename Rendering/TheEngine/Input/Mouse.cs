using System;
using Hexa.NET.ImGuizmo;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Input
{
    public class Mouse : IMouse
    {
        private readonly Engine engine;
        private volatile bool leftDown;
        private volatile bool rightDown;
        private volatile bool middleDown;

        private volatile bool leftJustDown;
        private volatile bool rightJustDown;
        private volatile bool middleJustDown;

        private volatile bool leftJustUp;
        private volatile bool rightJustUp;
        private volatile bool middleJustUp;

        private Vector2 mouseWheelDelta;

        private Vector2 lastPosition;
        public Vector2 Position { get; private set; }
        public Vector2 Delta { get; private set; }

        public Vector2 WheelDelta => mouseWheelDelta;

        // wheel gated the same way clicks are: only when the pointer is really over the 3D world
        // (not over the in-view toolbar/inspector chrome) - for camera zoom/fly-speed
        public Vector2 ViewWheelDelta => engine.gameView.IsHovered && !engine.gameView.IsPointerOverOverlay
            ? mouseWheelDelta
            : Vector2.Zero;
        public Vector2 RawNormalizedPosition { get; private set; }

        public Vector2 NormalizedPosition
        {
            get
            {
                var rawScreenPoint = RawScreenPoint;
                var gameRect = engine.gameView.ViewRect;
                if (gameRect.Contains(rawScreenPoint))
                {
                    var relativeX = (rawScreenPoint.X - gameRect.X) / gameRect.Width;
                    var relativeY = (rawScreenPoint.Y - gameRect.Y) / gameRect.Height;
                    return new Vector2(relativeX, relativeY);
                }
                else
                {
                    return default;
                }
            }
        }
        public Vector2 ScreenPoint
        {
            get
            {
                var zeroOne = new Vector2(NormalizedPosition.X, 1 - NormalizedPosition.Y);
                var gameRect = engine.gameView.ViewRect;
                return new Vector2(gameRect.Width * zeroOne.X, gameRect.Height * zeroOne.Y);
            }
        }
        public Vector2 RawScreenPoint
        {
            get
            {
                var zeroOne = new Vector2(RawNormalizedPosition.X, 1 - RawNormalizedPosition.Y);
                return new Vector2(engine.WindowHost.WindowWidth * zeroOne.X / engine.WindowHost.DpiScaling, engine.WindowHost.WindowHeight * zeroOne.Y / engine.WindowHost.DpiScaling);
            }
        }
        public Vector2 LastClickScreenPosition
        {
            get
            {
                var zeroOne = new Vector2(LastClickNormalizedPosition.X, 1 - LastClickNormalizedPosition.Y);
                var gameRect = engine.gameView.ViewRect;
                return new Vector2(gameRect.Width * zeroOne.X, gameRect.Height * zeroOne.Y);
            }
        }
        

        private Vector2 lastClickPosition;
        private float resetClickTimeCounter = 0;
        
        public uint ClickCount { get; private set; }

        public bool HasJustDoubleClicked => ClickCount == 2;
        public Vector2 LastClickNormalizedPosition { get; private set; }

        internal Mouse(Engine engine)
        {
            this.engine = engine;
        }
        
        internal void Update(float deltaMs)
        {
            Delta = lastPosition - Position;
            lastPosition = Position;
            if (resetClickTimeCounter > 0)
            {
                if (resetClickTimeCounter <= deltaMs)
                {
                    ClickCount = 0;
                    resetClickTimeCounter = 0;
                }
                else
                    resetClickTimeCounter -= deltaMs;
            }
        }

        internal void MouseWheel(Vector2 delta)
        {
            mouseWheelDelta = delta;
        }

        internal void MouseDown(MouseButton button)
        {
            if ((button & MouseButton.Left) != 0)
            {
                leftDown = true;
                leftJustDown = true;
            }

            if ((button & MouseButton.Right) != 0)
            {
                rightDown = true;
                rightJustDown = true;
            }

            if ((button & MouseButton.Middle) != 0)
            {
                middleDown = true;
                middleJustDown = true;
            }

            // middle presses stay out of the double-click counter on purpose
            if (leftJustDown || rightJustDown)
            {
                if ((lastClickPosition - ScreenPoint).LengthSquared() > 10 * 10)
                    ClickCount = 0;
                
                ClickCount++;
                resetClickTimeCounter = 500;
                lastClickPosition = ScreenPoint;
                LastClickNormalizedPosition = NormalizedPosition;
            }
        }

        internal void MouseUp(MouseButton button)
        {
            leftJustUp = leftDown && (button & MouseButton.Left) == 0;
            rightJustUp = rightDown && (button & MouseButton.Right) == 0;
            middleJustUp = middleDown && (button & MouseButton.Middle) == 0;
            leftDown = ((button & MouseButton.Left) != 0);
            rightDown = ((button & MouseButton.Right) != 0);
            middleDown = ((button & MouseButton.Middle) != 0);
        }

        public bool IsMouseDown(MouseButton button) => RawIsMouseDown(button) && engine.gameView.IsHovered;

        public bool RawIsMouseDown(MouseButton button)
        {
            if ((button & MouseButton.Left) != 0 && leftDown)
                return true;
            if ((button & MouseButton.Right) != 0 && rightDown)
                return true;
            return (button & MouseButton.Middle) != 0 && middleDown;
        }

        // IsPointerOverOverlay: UI drawn inside the game view (the in-view toolbar) claims clicks
        public bool HasJustClicked(MouseButton button) => RawHasJustClicked(button) && engine.gameView.IsHovered && !engine.gameView.IsPointerOverOverlay && !ImGuizmo.IsUsing() && !ImGuizmo.IsViewManipulateHovered();

        public bool RawHasJustClicked(MouseButton button)
        {
            if ((button & MouseButton.Left) != 0 && leftJustDown)
                return true;
            if ((button & MouseButton.Right) != 0 && rightJustDown)
                return true;
            return (button & MouseButton.Middle) != 0 && middleJustDown;
        }

        public bool HasJustReleased(MouseButton button) => RawHasJustReleased(button) && engine.gameView.IsHovered;
        
        public bool RawHasJustReleased(MouseButton button)
        {
            if ((button & MouseButton.Left) != 0 && leftJustUp)
                return true;
            if ((button & MouseButton.Right) != 0 && rightJustUp)
                return true;
            return (button & MouseButton.Middle) != 0 && middleJustUp;
        }

        public void PointerMoved(double x, double y, double width, double height)
        {
            Position = new Vector2((float)x, (float)y);
            RawNormalizedPosition = new Vector2((float)(x / width), 1 - (float)(y / height));
        }

        public void PostUpdate()
        {
            leftJustDown = false;
            rightJustDown = false;
            middleJustDown = false;
            leftJustUp = false;
            rightJustUp = false;
            middleJustUp = false;
            mouseWheelDelta = Vector2.Zero;
        }
    }

    [Flags]
    public enum MouseButton
    {
        None = 0,
        Left = 1,
        Right = 2,
        Middle = 4,
    }
}
