using Avalonia.Input;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Input
{
    internal class Keyboard : IKeyboard
    {
        private readonly Engine engine;
        internal volatile bool[] downKeys = new bool[255];
        internal Key[] justPressedKeys = new Key[20];
        internal Key[] justReleasedKeys = new Key[20];
        internal char[] justTextInput = new char[20];
        internal int justPressedKeysIndex = 0;
        internal int justReleasedKeysIndex = 0;
        internal int justTextInputIndex = 0;

        public Keyboard(Engine engine)
        {
            this.engine = engine;
        }

        internal void PostUpdate()
        {
            justPressedKeysIndex = 0;
            justReleasedKeysIndex = 0;
            justTextInputIndex = 0;
        }
        
        internal void KeyDown(Key key)
        {
            if (key >= 0 && (int)key <= 255)
            {
                //if (downKeys[(int)key])
                //    return;
                downKeys[(int)key] = true;
            }
            
            if (justPressedKeysIndex < justPressedKeys.Length)
                justPressedKeys[justPressedKeysIndex++] = key;
        }

        internal void KeyUp(Key key)
        {
            if (key >= 0 && (int)key <= 255)
            {
                //if (!downKeys[(int)key])
                //    return;
                downKeys[(int)key] = false;
            }
            
            if (justReleasedKeysIndex < justReleasedKeys.Length)
                justReleasedKeys[justReleasedKeysIndex++] = key;
        }
        
        public bool RawIsDown(Key keys)
        {
            return downKeys[(int)keys];
        }

        public bool IsDown(Key keys) => RawIsDown(keys) && engine.gameView.HasFocus;
        
        public bool JustPressed(Key key)
        {
            for (int i = 0; i < justPressedKeysIndex; ++i)
                if (justPressedKeys[i] == key)
                    return true;
            return false;
        }
        
        public bool JustReleased(Key key)
        {
            for (int i = 0; i < justReleasedKeysIndex; ++i)
                if (justReleasedKeys[i] == key)
                    return true;
            return false;
        }

        public Vector3 GetAxis(Vector3 axis, Key positive, Key negative) => engine.gameView.HasFocus
            ? RawGetAxis(axis, positive, negative)
            : default;
        
        public Vector3 RawGetAxis(Vector3 axis, Key positive, Key negative)
        {
            return axis * (RawIsDown(positive) ? 1 : 0) + axis * (RawIsDown(negative) ? -1 : 0);
        }

        public void ReleaseAllKeys()
        {
            for (int i = 0; i < downKeys.Length; ++i)
                downKeys[i] = false;
        }

        public void OnTextInput(char c)
        {
            if (justTextInputIndex >= justTextInput.Length)
                Array.Resize(ref justTextInput, justTextInput.Length * 2 + 1);
            justTextInput[justTextInputIndex++] = c;
        }
    }
}
