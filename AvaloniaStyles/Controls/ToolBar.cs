using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace AvaloniaStyles.Controls
{
    public class ToolBar : Control
    {
        public static readonly StyledProperty<object> ContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(Content));
        
        public static readonly StyledProperty<object> LeftContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(LeftContent));
        
        public static readonly StyledProperty<object> RightContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(RightContent));
        
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
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

        static ToolBar()
        {
            ContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            LeftContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            RightContentProperty.Changed.AddClassHandler<ToolBar>(Action);
        }

        private static void Action(ToolBar titlebar, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.OldValue is ILogical oldLogical) 
                titlebar.LogicalChildren.Remove(oldLogical);

            if (args.NewValue is ILogical newLogical) 
                titlebar.LogicalChildren.Add(newLogical);
        }
    }
}