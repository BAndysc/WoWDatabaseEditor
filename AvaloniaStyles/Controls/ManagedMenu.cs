using Avalonia;
using Avalonia.Controls;

namespace AvaloniaStyles.Controls;

/// <summary>
/// for some reason, .net 7.0 and this old Avalonia version doesn't like the NativeMenu class
/// probably in Avalonia 11 it is fixed, but for now I've disabled NativeMenu
/// for Ava11, remove this and change all ManagedMenu references to NativeMenu
/// </summary>
public class ManagedMenu
{
    public static readonly AttachedProperty<NativeMenu> MenuProperty
        = AvaloniaProperty.RegisterAttached<ManagedMenu, AvaloniaObject, NativeMenu>("Menu");

    public static void SetMenu(AvaloniaObject o, NativeMenu menu) => o.SetValue(MenuProperty, menu);

    public static NativeMenu GetMenu(AvaloniaObject o) => o.GetValue(MenuProperty);
    
    static ManagedMenu()
    {
    }
}