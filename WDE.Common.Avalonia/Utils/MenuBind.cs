using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
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
                        item = new NativeMenuItem(subItem.ItemName.Replace("_", ""));
                        if (subItem is IMenuCommandItem cmd)
                            ((NativeMenuItem) item).Command = cmd.ItemCommand;
                    }
                    
                    topLevelItem.Menu.Add(item);
                }
                
                (targetLocation as NativeMenu).Add(topLevelItem);
            }
            return viewModel;
        }
    }
}