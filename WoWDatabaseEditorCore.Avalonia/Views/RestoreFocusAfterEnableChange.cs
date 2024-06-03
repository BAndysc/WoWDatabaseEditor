using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Utils;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Avalonia.Views;

public class RestoreFocusAfterEnableChange
{
    private WeakReference<Control>? lastFocused;
    private IDisposable? enabledDisposable;
    private IFocusManager? focusManager;

    public RestoreFocusAfterEnableChange()
    {
        InputElement.GotFocusEvent.AddClassHandler<Control>(OnGotFocus);
        InputElement.LostFocusEvent.AddClassHandler<Control>(OnLostFocus);
    }

    private void OnLostFocus(Control control, RoutedEventArgs e)
    {
    }

    private void OnGotFocus(Control control, GotFocusEventArgs e)
    {
        if (focusManager == null)
        {
            focusManager = Application.Current?.GetTopLevel()?.FocusManager;
            if (focusManager == null)
                return;
        }

        var focusedElement = focusManager.GetFocusedElement();

        if (lastFocused != null && lastFocused.TryGetTarget(out var lastFocusedControl))
        {
            if (ReferenceEquals(focusedElement, lastFocusedControl))
                return;

            lastFocusedControl.DetachedFromVisualTree -= OnLastFocusedDetachedFromVisualTree;
            enabledDisposable?.Dispose();
            lastFocused = null;
            enabledDisposable = null;
        }

        if (focusedElement is not Control focusedElementControl)
            return;

        lastFocused = new WeakReference<Control>(focusedElementControl);
        focusedElementControl.DetachedFromVisualTree += OnLastFocusedDetachedFromVisualTree;
        enabledDisposable = focusedElementControl.GetObservable(InputElement.IsEffectivelyEnabledProperty).Skip(1).SubscribeAction(@is =>
        {
            if (@is && lastFocused.TryGetTarget(out var lastFocusedControl) && lastFocusedControl == focusedElementControl)
            {
                focusedElementControl.Focus();
            }
        });
    }

    private void OnLastFocusedDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (lastFocused != null && lastFocused.TryGetTarget(out var lastFocusedControl))
        {
            lastFocusedControl.DetachedFromVisualTree -= OnLastFocusedDetachedFromVisualTree;
            enabledDisposable?.Dispose();
            lastFocused = null;
            enabledDisposable = null;
        }
    }
}