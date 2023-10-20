using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace WDE.Common.Avalonia.Utils;

public class ControlPointerReleasedTriggerBehavior : Trigger
{
    private IDisposable? dispose;

    protected override void OnAttached()
    {
        base.OnAttached();
        dispose?.Dispose();
        dispose = null;
        if (AssociatedObject is Control c)
        {
            dispose = c.AddDisposableHandler(InputElement.PointerReleasedEvent, OnEvent, RoutingStrategies.Tunnel);
        }
    }

    private void OnEvent(object? sender, PointerReleasedEventArgs e)
    {
        if (e.KeyModifiers.HasFlagFast(KeyModifiers.Control) || e.KeyModifiers.HasFlagFast(KeyModifiers.Meta))
        {
            Interaction.ExecuteActions(AssociatedObject, Actions, e);
            e.Handled = true;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        dispose?.Dispose();
        dispose = null;
    }
}