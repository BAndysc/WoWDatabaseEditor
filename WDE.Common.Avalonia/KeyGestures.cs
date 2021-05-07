using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace WDE.Common.Avalonia
{
    public static class KeyGestures
    {
        public static KeyGesture Cut { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Cut.FirstOrDefault();

        public static KeyGesture Copy { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Copy.FirstOrDefault();

        public static KeyGesture Paste { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Paste.FirstOrDefault();
        
        public static KeyGesture Undo { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Undo.FirstOrDefault();

        public static KeyGesture Redo { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Redo.FirstOrDefault();
    }
}