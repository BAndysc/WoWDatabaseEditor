using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace WDE.Common.Avalonia
{
    public static class KeyGestures
    {
        public static KeyGesture Cut { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Cut.FirstOrDefault() ?? new KeyGesture(Key.X, KeyModifiers.Control);

        public static KeyGesture Copy { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Copy.FirstOrDefault() ?? new KeyGesture(Key.C, KeyModifiers.Control);

        public static KeyGesture Paste { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Paste.FirstOrDefault() ?? new KeyGesture(Key.V, KeyModifiers.Control);
        
        public static KeyGesture Undo { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Undo.FirstOrDefault() ?? new KeyGesture(Key.Z, KeyModifiers.Control);

        public static KeyGesture Redo { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Redo.FirstOrDefault() ?? new KeyGesture(Key.Y, KeyModifiers.Control);
    }
}