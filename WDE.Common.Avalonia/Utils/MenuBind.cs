using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using WDE.Common.Menu;

namespace WDE.Common.Avalonia.Utils
{
    public static class MenuBind
    {
        public static readonly AvaloniaProperty MenuItemsProperty = AvaloniaProperty.RegisterAttached<NativeMenu, IList<IMainMenuItem>>("Model",
            typeof(MenuBind),coerce: OnMenuChanged);
        
        public static IList<IMainMenuItem> GetMenuItems(NativeMenu control) => (IList<IMainMenuItem>)control.GetValue(MenuItemsProperty);
        public static void SetMenuItems(NativeMenu control, object value) => control.SetValue(MenuItemsProperty, value);
        
        private static IList<IMainMenuItem> OnMenuChanged(IAvaloniaObject targetLocation, IList<IMainMenuItem> viewModel)
        {
            var systemWideControlModifier = AvaloniaLocator.Current
                .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;
            
            NativeMenuItemBase item;
            foreach (var m in viewModel)
            {
                NativeMenuItem topLevelItem = new NativeMenuItem(m.ItemName.Replace("_", ""));
                topLevelItem.Menu = new NativeMenu();
                foreach (var subItem in m.SubItems)
                {
                    if (subItem.ItemName == "Separator")
                        item = new NativeMenuItemSeperator();
                    else
                    {
                        var nativeMenuItem = new NativeMenuItem(subItem.ItemName.Replace("_", ""));
                        if (subItem is IMenuCommandItem cmd)
                        {
                            nativeMenuItem.Command = cmd.ItemCommand;
                            if (cmd.Shortcut.HasValue && Enum.TryParse(cmd.Shortcut.Value.Key, out Key key))
                            { 
                                var keyGesture = new KeyGesture(key, cmd.Shortcut.Value.Control ? systemWideControlModifier : KeyModifiers.None);
                                nativeMenuItem.Gesture = keyGesture;
                            }
                        }
                        item = nativeMenuItem;
                    }
                    
                    topLevelItem.Menu.Add(item);
                }
                
                (targetLocation as NativeMenu).Add(topLevelItem);
            }
            return viewModel;
        }
        
        
        public static readonly AvaloniaProperty MenuItemsGesturesProperty = AvaloniaProperty.RegisterAttached<Window, IList<IMainMenuItem>>("MenuItemsGestures",
            typeof(MenuBind),coerce: OnMenuGesturesChanged);
        
        public static IList<IMainMenuItem> GetMenuItemsGestures(Window control) => (IList<IMainMenuItem>)control.GetValue(MenuItemsGesturesProperty);
        public static void SetMenuItemsGestures(Window control, object value) => control.SetValue(MenuItemsGesturesProperty, value);
        
        private static IList<IMainMenuItem> OnMenuGesturesChanged(IAvaloniaObject targetLocation, IList<IMainMenuItem> viewModel)
        {
            var systemWideControlModifier = AvaloniaLocator.Current
                .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;

            var window = targetLocation as Window;
            if (window == null)
                return viewModel;

            foreach (var m in viewModel)
            {
                foreach (var subItem in m.SubItems)
                {
                    if (subItem is not IMenuCommandItem cmd)
                        continue;
                    
                    if (!cmd.Shortcut.HasValue || !Enum.TryParse(cmd.Shortcut.Value.Key, out Key key)) 
                        continue;
                    
                    var keyGesture = new KeyGesture(key, cmd.Shortcut.Value.Control ? systemWideControlModifier : KeyModifiers.None);
                    window.KeyBindings.Add(new KeyBinding(){Command = cmd.ItemCommand, Gesture = keyGesture});
                }
            }
            return viewModel;
        }
    }
}