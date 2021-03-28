using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace AvaloniaStyles.Controls
{
    public class ExtendedTitleBar : ContentControl
    {
        public static readonly StyledProperty<object> LeftContentProperty =
            AvaloniaProperty.Register<ExtendedTitleBar, object>(nameof(LeftContent));
        public static readonly StyledProperty<object> RightContentProperty =
            AvaloniaProperty.Register<ExtendedTitleBar, object>(nameof(RightContent));
        
        public ExtendedTitleBar()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                PseudoClasses.Add(":macos");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                PseudoClasses.Add(":windows");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                PseudoClasses.Add(":linux");
        }
        
        public object LeftContent
        {
            get => GetValue(LeftContentProperty);
            set => SetValue(LeftContentProperty, value);
        }
        
        public object RightContent
        {
            get => GetValue(RightContentProperty);
            set => SetValue(RightContentProperty, value);
        }

        static ExtendedTitleBar()
        {
            LeftContentProperty.Changed.AddClassHandler<ExtendedTitleBar>(Action);
            RightContentProperty.Changed.AddClassHandler<ExtendedTitleBar>(Action);
        }

        private static void Action(ExtendedTitleBar titlebar, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.OldValue is ILogical oldLogical) 
                titlebar.LogicalChildren.Remove(oldLogical);

            if (args.NewValue is ILogical newLogical) 
                titlebar.LogicalChildren.Add(newLogical);
        }
    }
}