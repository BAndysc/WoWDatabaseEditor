using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace AvaloniaStyles.Controls
{
    public class ToolBar : Control
    {
        public static readonly StyledProperty<object> MiddleContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(MiddleContent));
        
        public static readonly StyledProperty<object> LeftContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(LeftContent));
        
        public static readonly StyledProperty<object> RightContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(RightContent));
        
        public static readonly StyledProperty<object> TopContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(TopContent));
        
        public static readonly StyledProperty<object> TopLeftContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(TopLeftContent));

        public object MiddleContent
        {
            get => GetValue(MiddleContentProperty);
            set => SetValue(MiddleContentProperty, value);
        }
        
        public object LeftContent
        {
            get => GetValue(LeftContentProperty);
            set => SetValue(LeftContentProperty, value);
        }
        
        public object TopContent
        {
            get => GetValue(TopContentProperty);
            set => SetValue(TopContentProperty, value);
        }
        
        public object TopLeftContent
        {
            get => GetValue(TopLeftContentProperty);
            set => SetValue(TopLeftContentProperty, value);
        }

        public object RightContent
        {
            get => GetValue(RightContentProperty);
            set => SetValue(RightContentProperty, value);
        }

        static ToolBar()
        {
            MiddleContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            LeftContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            RightContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            TopContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            TopLeftContentProperty.Changed.AddClassHandler<ToolBar>(Action);
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