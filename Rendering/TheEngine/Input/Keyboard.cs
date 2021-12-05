using Avalonia.Input;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Input
{
    internal class Keyboard : IKeyboard
    {
        private volatile bool[] downKeys = new bool[255];
        private Key[] justPressedKeys = new Key[20];
        private int justPressedKeysIndex = 0;

        internal void PostUpdate()
        {
            justPressedKeysIndex = 0;
        }
        
        internal void KeyDown(Key key)
        {
            if (key >= 0 && (int)key <= 255)
            {
                if (downKeys[(int)key])
                    return;
                downKeys[(int)key] = true;
            }
            
            if (justPressedKeysIndex < justPressedKeys.Length)
                justPressedKeys[justPressedKeysIndex++] = key;
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
        
        public bool JustPressed(Key key)
        {
            for (int i = 0; i < justPressedKeysIndex; ++i)
                if (justPressedKeys[i] == key)
                    return true;
            return false;
        }
        
        public Vector3 GetAxis(Vector3 axis, Key positive, Key negative)
        {
            return axis * (IsDown(positive) ? 1 : 0) + axis * (IsDown(negative) ? -1 : 0);
        }

        public void ReleaseAllKeys()
        {
            for (int i = 0; i < downKeys.Length; ++i)
                downKeys[i] = false;
        }
    }
}
