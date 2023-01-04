using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace WDE.Common.Avalonia
{
    public static class KeyGestures
    {
        public static KeyModifiers CommandModifier { get; }
        
        static KeyGestures()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                CommandModifier = KeyModifiers.Meta;
            else
                CommandModifier = KeyModifiers.Control;

            Cut = new KeyGesture(Key.X, CommandModifier);
            Copy = new KeyGesture(Key.C, CommandModifier);
            Paste = new KeyGesture(Key.V, CommandModifier);
            Undo = new KeyGesture(Key.Z, CommandModifier);
            Redo = new KeyGesture(Key.Y, CommandModifier);
            Save = new KeyGesture(Key.S, CommandModifier);
        }
        
        public static KeyGesture Cut { get; }

        public static KeyGesture Copy { get; }

        public static KeyGesture Paste { get; }
        
        public static KeyGesture Undo { get; }

        public static KeyGesture Redo { get; }
        
        public static KeyGesture Save { get; }
    }
}