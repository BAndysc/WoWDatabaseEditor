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

        public bool IsDown(Key keys) => RawIsDown(keys) && (engine.gameView.HasFocus || engine.gameView.IsHovered);
        
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

        public Vector3 GetAxis(Vector3 axis, Key positive, Key negative) => (engine.gameView.HasFocus || engine.gameView.IsHovered)
            ? RawGetAxis(axis, positive, negative)
            : default;
        
        public Vector3 RawGetAxis(Vector3 axis, Key positive, Key negative)
        {
            return axis * (RawIsDown(positive) ? 1 : 0) + axis * (RawIsDown(negative) ? -1 : 0);
        }

        public void ReleaseAllKeys()
        {
            // Called on focus loss to prevent stuck movement keys (their KeyUp will never arrive).
            // Modifiers are KEPT: focus often blinks away for a moment mid-interaction (first spawn
            // creates the hosted documents, a toast pops, ...) and modifiers don't autorepeat, so a
            // force-released held Shift stayed "up" until physically re-pressed - which silently
            // broke hold-Shift multi-placement after the first spawn. A modifier physically
            // released while unfocused self-corrects on its next press.
            for (int i = 0; i < downKeys.Length; ++i)
            {
                var key = (Key)i;
                if (key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt)
                    continue;
                downKeys[i] = false;
            }
        }

        public void OnTextInput(char c)
        {
            if (justTextInputIndex >= justTextInput.Length)
                Array.Resize(ref justTextInput, justTextInput.Length * 2 + 1);
            justTextInput[justTextInputIndex++] = c;
        }
    }
}
