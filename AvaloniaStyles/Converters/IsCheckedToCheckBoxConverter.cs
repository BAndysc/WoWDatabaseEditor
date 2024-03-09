using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;

namespace AvaloniaStyles.Converters
{
    public class NativeMenuItemHelper
    {
        public static readonly AttachedProperty<bool> IsCheckedProperty =
            AvaloniaProperty.RegisterAttached<NativeMenuItemHelper, MenuItem, bool>(
                "IsChecked");
        public static readonly AttachedProperty<NativeMenuItemToggleType> ToggleTypeProperty =
            AvaloniaProperty.RegisterAttached<NativeMenuItemHelper, MenuItem, NativeMenuItemToggleType>(
                "ToggleType");

        static NativeMenuItemHelper()
        {
            IsCheckedProperty.Changed.Subscribe(args =>
            {
                var item = (MenuItem)args.Sender;
                Update(item);
            });
            ToggleTypeProperty.Changed.Subscribe(args =>
            {
                var item = (MenuItem)args.Sender;
                Update(item);
            });
        }

        private static void Update(MenuItem item)
        {
            var toggleType = GetToggleType(item);
            var @is = GetIsChecked(item);

            if (toggleType != NativeMenuItemToggleType.None)
            {
                item.Icon = new CheckBox()
                {
                    IsChecked = @is,
                    IsHitTestVisible = false,
                    BorderThickness = new Thickness(0)
                };
            }
            else
                item.Icon = null!;
        }

        public static void SetIsChecked(MenuItem menuItem, bool enable)
        {
            menuItem.SetValue(IsCheckedProperty, enable);
        }

        public static bool GetIsChecked(MenuItem menuItem)
        {
            return menuItem.GetValue(IsCheckedProperty);
        }
        
        public static void SetToggleType(MenuItem menuItem, NativeMenuItemToggleType enable)
        {
            menuItem.SetValue(ToggleTypeProperty, enable);
        }

        public static NativeMenuItemToggleType GetToggleType(MenuItem menuItem)
        {
            return menuItem.GetValue(ToggleTypeProperty);
        }
    }
    
    public class IsCheckedToCheckBoxConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isChecked = false;
            if (value is bool b)
                isChecked = b;

            return new CheckBox() { IsChecked = isChecked, IsHitTestVisible = false, BorderThickness = new Thickness(0) };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
