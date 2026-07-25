using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace TheEngine;

/// <summary>
/// Tracks whether the editor application is in the foreground, for the background fps cap
/// (Engine.UnfocusedFpsLimit). App-level activation (IActivatableLifetime - NSApplication
/// active state on macOS) is the correct signal for "the user switched to another app":
/// per-window Activated/Deactivated also fires when focus moves between the editor's OWN
/// windows (floating dock panels, dialogs), which must not throttle the 3D view. On platforms
/// where the lifetime feature is unavailable (Win32/X11 desktop) callers fall back to their
/// TopLevel window's activation state via <see cref="IsEditorFocused"/>.
/// </summary>
internal static class AppActivationTracker
{
    private static bool initialized;
    private static bool appLevelAvailable;
    // written on the UI thread, read by render threads - plain bool per threading policy
    private static volatile bool appActive = true;

    /// <summary>Subscribes to app activation events; call from the UI thread (panel attach).</summary>
    public static void EnsureInitialized()
    {
        if (initialized)
            return;
        initialized = true;
        if (Application.Current?.TryGetFeature<IActivatableLifetime>() is { } lifetime)
        {
            appLevelAvailable = true;
            lifetime.Activated += (_, e) =>
            {
                if (e.Kind == ActivationKind.Background)
                    appActive = true;
            };
            lifetime.Deactivated += (_, e) =>
            {
                if (e.Kind == ActivationKind.Background)
                    appActive = false;
            };
        }
    }

    /// <summary>Effective "the editor is in the foreground" signal: app-level when available,
    /// otherwise the caller's own TopLevel window activation state.</summary>
    public static bool IsEditorFocused(bool windowActive) => appLevelAvailable ? appActive : windowActive;
}
