using System;
using Avalonia;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Input
{
    public class Mouse : IMouse
    {
        private readonly Engine engine;
        private volatile bool leftDown;
        private volatile bool rightDown;
        
        private volatile bool leftJustDown;
        private volatile bool rightJustDown;
        
        private volatile bool leftJustUp;
        private volatile bool rightJustUp;

        private Vector2 mouseWheelDelta;

        private Vector2 lastPosition;
        public Vector2 Position { get; private set; }
        public Vector2 Delta { get; private set; }

        public Vector2 WheelDelta => mouseWheelDelta;
        public Vector2 NormalizedPosition { get; private set; }
        public Vector2 ScreenPoint
        {
            get
            {
                var zeroOne = new Vector2(NormalizedPosition.X, 1 - NormalizedPosition.Y);
                return new Vector2(engine.WindowHost.WindowWidth * zeroOne.X, engine.WindowHost.WindowHeight * zeroOne.Y);
            }
        }
        public Vector2 LastClickScreenPosition
        {
            get
            {
                var zeroOne = new Vector2(LastClickNormalizedPosition.X, 1 - LastClickNormalizedPosition.Y);
                return new Vector2(engine.WindowHost.WindowWidth * zeroOne.X, engine.WindowHost.WindowHeight * zeroOne.Y);
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
            leftDown = ((button & MouseButton.Left) != 0);
            rightDown = ((button & MouseButton.Right) != 0);
        }

        public bool IsMouseDown(MouseButton button)
        {
            if (((int)button & (int)MouseButton.Left) > 0)
                return leftDown;

            return rightDown;
        }
        
        public bool HasJustClicked(MouseButton button)
        {
            if (((int)button & (int)MouseButton.Left) > 0)
                return leftJustDown;

            return rightJustDown;
        }
        
        public bool HasJustReleased(MouseButton button)
        {
            if (((int)button & (int)MouseButton.Left) > 0)
                return leftJustUp;

            return rightJustUp;
        }

        public void PointerMoved(double x, double y, double width, double height)
        {
            Position = new Vector2((float)x, (float)y);
            NormalizedPosition = new Vector2((float)(x / width), 1 - (float)(y / height));
        }

        public void PostUpdate()
        {
            leftJustDown = false;
            rightJustDown = false;
            leftJustUp = false;
            rightJustUp = false;
            mouseWheelDelta = Vector2.Zero;
        }
    }

    [Flags]
    public enum MouseButton
    {
        None = 0,
        Left = 1,
        Right = 2,
    }
}
