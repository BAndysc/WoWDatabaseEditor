using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Utils;

public static class KeyGestureBind
{
    public static readonly AvaloniaProperty BindingsProperty = AvaloniaProperty.RegisterAttached<UserControl, IList<CommandKeyBinding>>("Bindings",
        typeof(KeyGestureBind));

    static KeyGestureBind()
    {
        BindingsProperty.Changed.AddClassHandler<UserControl>((control, e) =>
        {
            control.KeyBindings.Clear();
            if (e.NewValue is IList<CommandKeyBinding> bindings)
            {
                foreach (var binding in bindings)
                {
                    var b = new BetterKeyBinding()
                    {
                        CustomCommand = binding.Command,
                        Gesture = KeyGesture.Parse(binding.KeyGesture)
                    };
                    if (binding.HighestPriority)
                        b.Command = binding.Command;
                    control.KeyBindings.Add(b);
                }
            }
        });
    }
    
    public static IList<CommandKeyBinding> GetBindings(UserControl control) => (IList<CommandKeyBinding>?)control.GetValue(BindingsProperty)??new List<CommandKeyBinding>();
    public static void SetBindings(UserControl control, object value) => control.SetValue(BindingsProperty, value);

}
