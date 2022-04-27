using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace WDE.Common.Avalonia.Utils
{
    public class PressedClassBehaviour : Behavior<Control>
    {
        protected override void OnAttached()
        {
            AssociatedObject?.AddHandler(InputElement.PointerPressedEvent, PressedHandler, RoutingStrategies.Tunnel);
            AssociatedObject?.AddHandler(InputElement.PointerReleasedEvent, ReleasedHandler, RoutingStrategies.Tunnel);
        }

        protected override void OnDetaching()
        {
            AssociatedObject?.RemoveHandler(InputElement.PointerPressedEvent, PressedHandler);
            AssociatedObject?.RemoveHandler(InputElement.PointerReleasedEvent, ReleasedHandler);
        }

        private void PressedHandler(object? sender, PointerPressedEventArgs e)
        {
            var control = (sender as Control);
            ((IPseudoClasses?)control?.Classes)?.Set(":pressed", true);
        }
        
        private void ReleasedHandler(object? sender, PointerReleasedEventArgs e)
        {
            var control = (sender as Control);
            ((IPseudoClasses?)control?.Classes)?.Set(":pressed", false);
        }
    }
}