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

        private volatile short mouseWheelDelta;

        private Vector2 lastPosition;
        public Vector2 Position { get; private set; }
        public Vector2 Delta { get; private set; }

        public short WheelDelta => mouseWheelDelta;
        public Vector2 NormalizedPosition { get; private set; }
        public Vector2 ScreenPoint
        {
            get
            {
                var zeroOne = new Vector2(NormalizedPosition.X, 1 - NormalizedPosition.Y);
                return new Vector2(engine.WindowHost.WindowWidth * zeroOne.X, engine.WindowHost.WindowHeight * zeroOne.Y);
            }
        }

        internal Mouse(Engine engine)
        {
            this.engine = engine;
        }
        
        internal void Update()
        {
            Delta = lastPosition - Position;
            lastPosition = Position;
            mouseWheelDelta = 0;
        }

        internal void MouseWheel(short delta)
        {
            mouseWheelDelta = delta;
        }

        internal void MouseDown(MouseButton button)
        {
            if (button.HasFlag(MouseButton.Left))
            {
                leftDown = true;
                leftJustDown = true;
            }

            if (button.HasFlag(MouseButton.Right))
            {
                rightDown = true;
                rightJustDown = true;
            }
        }

        internal void MouseUp(MouseButton button)
        {
            leftDown = button.HasFlag(MouseButton.Left);
            rightDown = button.HasFlag(MouseButton.Right);
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

        public void PointerMoved(double x, double y, double width, double height)
        {
            Position = new Vector2((float)x, (float)y);
            NormalizedPosition = new Vector2((float)(x / width), 1 - (float)(y / height));
        }

        public void PostUpdate()
        {
            leftJustDown = false;
            rightJustDown = false;
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
