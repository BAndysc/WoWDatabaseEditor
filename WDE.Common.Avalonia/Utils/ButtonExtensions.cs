using Avalonia;
using Avalonia.Controls;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Utils;

public static class ButtonExtensions
{
    public static readonly AvaloniaProperty ButtonFlyoutIsOpenedProperty = AvaloniaProperty.RegisterAttached<Button, bool>("ButtonFlyoutIsOpened", typeof(ButtonExtensions));

    public static bool GetButtonFlyoutIsOpened(Button control) => (bool)(control.GetValue(ButtonFlyoutIsOpenedProperty) ?? false);
    public static void SetButtonFlyoutIsOpened(Button control, bool value) => control.SetValue(ButtonFlyoutIsOpenedProperty, value);

    static ButtonExtensions()
    {
        ButtonFlyoutIsOpenedProperty.Changed.SubscribeAction(args =>
        {
            if (args.Sender is Button b)
            {
                if (args.NewValue is true)
                    b.Flyout?.ShowAt(b);
                else
                    b.Flyout?.Hide();
            }
        });
    }
}