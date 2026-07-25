using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Avalonia.Services.FocusService;

/// <summary>
/// The editor counts as focused when any of its windows (main window,
/// popped out documents, dialogs) is the active OS window.
/// </summary>
[AutoRegister(Platforms.Desktop)]
[SingleInstance]
public class EditorFocusService : IEditorFocusService
{
    private bool isFocused;

    public bool IsEditorFocused => isFocused;
    public event Action<bool>? FocusChanged;

    public EditorFocusService()
    {
        isFocused = AnyWindowActive();
        WindowBase.IsActiveProperty.Changed.Subscribe(OnWindowIsActiveChanged);
    }

    private void OnWindowIsActiveChanged(AvaloniaPropertyChangedEventArgs<bool> e)
    {
        var nowFocused = e.NewValue.GetValueOrDefault() || AnyWindowActive();
        if (nowFocused == isFocused)
            return;

        isFocused = nowFocused;
        FocusChanged?.Invoke(nowFocused);
    }

    private static bool AnyWindowActive()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            foreach (var window in desktop.Windows)
            {
                if (window.IsActive)
                    return true;
            }
            return false;
        }

        // single view (browser/mobile) - there is no concept of an inactive window here
        return true;
    }
}
