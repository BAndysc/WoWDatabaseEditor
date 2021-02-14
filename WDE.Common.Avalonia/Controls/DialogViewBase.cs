using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace WDE.Common.Avalonia.Controls
{
    public class DialogViewBase : UserControl
    {
        public static readonly StyledProperty<IControl> ToolBarProperty =
            AvaloniaProperty.Register<DialogViewBase, IControl>(nameof(ToolBar));
        
        public IControl ToolBar
        {
            get => GetValue(ToolBarProperty);
            set => SetValue(ToolBarProperty, value);
        }
        
        static DialogViewBase()
        {
            ToolBarProperty.Changed.AddClassHandler<DialogViewBase>((x, e) => x.ContentChanged(e, ":has-toolbar"));
        }

        private void ContentChanged(AvaloniaPropertyChangedEventArgs e, string pseudoclass)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
                PseudoClasses.Remove(pseudoclass);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
                PseudoClasses.Add(pseudoclass);
            }
        }
    }
}