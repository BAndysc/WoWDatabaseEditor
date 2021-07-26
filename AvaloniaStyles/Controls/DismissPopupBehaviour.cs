using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace AvaloniaStyles.Controls
{
    public class DismissPopupBehaviour : Behavior<Control>
    {
        protected override void OnAttached()
        {
            AssociatedObject?.AddHandler(InputElement.PointerReleasedEvent, Handler, RoutingStrategies.Tunnel);
        }

        protected override void OnDetaching()
        {
            AssociatedObject?.RemoveHandler(InputElement.PointerReleasedEvent, Handler);
        }

        private void Handler(object? sender, PointerReleasedEventArgs e)
        {
            var control = (sender as Control);
            var popup = control.FindLogicalAncestorOfType<Popup>();
            if (popup != null)
                popup.IsOpen = false;
        }
    }
}