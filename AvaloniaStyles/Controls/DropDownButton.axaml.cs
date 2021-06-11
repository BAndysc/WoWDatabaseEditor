using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace AvaloniaStyles.Controls
{
    public class DropDownButton : TemplatedControl
    {
        private object? button;
        public static readonly DirectProperty<DropDownButton, object?> ButtonProperty = AvaloniaProperty.RegisterDirect<DropDownButton, object?>("Button", o => o.Button, (o, v) => o.Button = v);
        private bool isDropDownOpened;
        public static readonly DirectProperty<DropDownButton, bool> IsDropDownOpenedProperty = AvaloniaProperty.RegisterDirect<DropDownButton, bool>("IsDropDownOpened", o => o.IsDropDownOpened, (o, v) => o.IsDropDownOpened = v);

        public static readonly StyledProperty<Control?> ChildProperty =
            AvaloniaProperty.Register<DropDownButton, Control?>(nameof(Child));
        
        public object? Button
        {
            get => button;
            set => SetAndRaise(ButtonProperty, ref button, value);
        }

        public bool IsDropDownOpened
        {
            get => isDropDownOpened;
            set => SetAndRaise(IsDropDownOpenedProperty, ref isDropDownOpened, value);
        }
        
        [Content]
        public Control? Child
        {
            get => GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }
    }
}