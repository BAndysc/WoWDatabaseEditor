using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using WDE.MVVM.Observable;

namespace WDE.Common.Avalonia.Utils;

public static class Fix
{
    /// <summary>
    /// This is a workaround for avalonia bug https://github.com/AvaloniaUI/Avalonia/issues/9940
    /// </summary>
    public static readonly AvaloniaProperty DetachDataContextProperty = AvaloniaProperty.RegisterAttached<Control, bool>("DetachDataContext", typeof(Fix));
        
    public static bool GetDetachDataContext(Control control) => (bool)(control.GetValue(DetachDataContextProperty) ?? false);
    public static void SetDetachDataContext(Control control, bool value) => control.SetValue(DetachDataContextProperty, value);

    static Fix()
    {
        DetachDataContextProperty.Changed.SubscribeAction(args =>
        {
            if (args.Sender is Control c)
            {
                c.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
            }
        });
    }

    private static void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        if (sender is Control c)
            c.DataContext = null;
    }
}
