using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WDE.Common.Menu;

namespace WoWDatabaseEditorCore.WPF.Extensions
{
    public static class GlobalMenuHotKeys
    {

        public static readonly DependencyProperty MenuItemsGesturesProperty = DependencyProperty.RegisterAttached("MenuItemsGestures",
            typeof(IList<IMainMenuItem>),
            typeof(GlobalMenuHotKeys),
            new PropertyMetadata(null, OnHotKeyPropertyChanged));

        private static void OnHotKeyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window == null)
                return;

            var menus = GetMenuItemsGestures(window);

            if (menus == null)
                return;

            foreach (var m in menus)
            {
                foreach (var subItem in m.SubItems)
                {
                    if (subItem is not IMenuCommandItem cmd)
                        continue;

                    if (!cmd.Shortcut.HasValue || !Enum.TryParse(cmd.Shortcut.Value.Key, out Key key))
                        continue;

                    var keyGesture = new KeyGesture(key, cmd.Shortcut.Value.Control ? ModifierKeys.Control : ModifierKeys.None);
                    window.InputBindings.Add(new InputBinding(cmd.ItemCommand, keyGesture));
                }
            }
        }

        public static IList<IMainMenuItem>? GetMenuItemsGestures(Control control)
        {
            return (IList<IMainMenuItem>?) control.GetValue(MenuItemsGesturesProperty);
        }

        public static void SetMenuItemsGestures(Control control, IList<IMainMenuItem>? value)
        {
            control.SetValue(MenuItemsGesturesProperty, value);
        }   
    }
}