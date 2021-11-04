using Avalonia.Input;
using TheMaths;

namespace TheEngine.Input
{
    internal class Keyboard : IKeyboard
    {
        private volatile bool[] downKeys = new bool[255];

        internal void KeyDown(Key key)
        {
            if (key >= 0 && (int)key <= 255)
                downKeys[(int)key] = true;
        }

        internal void KeyUp(Key key)
        {
            if (key >= 0 && (int)key <= 255)
                downKeys[(int)key] = false;
        }
        public bool IsDown(Key keys)
        {
            return downKeys[(int)keys];
        }

        public Vector3 GetAxis(Vector3 axis, Key positive, Key negative)
        {
            return axis * (IsDown(positive) ? 1 : 0) + axis * (IsDown(negative) ? -1 : 0);
        }
    }
}
