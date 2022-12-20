using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace AvaloniaStyles.Behaviours;

public class OpenContextMenuOnClickBehavior : Behavior<Button>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
            AssociatedObject.Click += AssociatedObject_PointerPressed;
    }

    private void AssociatedObject_PointerPressed(object? sender, RoutedEventArgs routedEventArgs)
    {
        AssociatedObject?.ContextMenu?.Open();
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
            AssociatedObject.Click -= AssociatedObject_PointerPressed;
        base.OnDetaching();
    }
}


public class OpenContextMenuOnLeftMouseButtonBehavior : Behavior<Control>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
            AssociatedObject.PointerPressed += AssociatedObject_PointerPressed;
    }

    private void AssociatedObject_PointerPressed(object? sender, PointerPressedEventArgs routedEventArgs)
    {
        AssociatedObject?.ContextMenu?.Open();
        AssociatedObject?.ContextFlyout?.ShowAt(AssociatedObject!);
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
            AssociatedObject.PointerPressed -= AssociatedObject_PointerPressed;
        base.OnDetaching();
    }
}