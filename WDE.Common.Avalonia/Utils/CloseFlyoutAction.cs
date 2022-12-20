using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Xaml.Interactivity;

namespace WDE.Common.Avalonia.Utils;

public class CloseFlyoutAction : AvaloniaObject, IAction
{
    public object? Execute(object? sender, object? parameter)
    {
        if (sender is ILogical control)
        {
            var popup = control.FindLogicalAncestorOfType<Popup>();
            if (popup != null)
                popup.IsOpen = false;
        }

        return null;
    }
}