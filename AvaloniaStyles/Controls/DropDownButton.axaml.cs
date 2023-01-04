using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace AvaloniaStyles.Controls
{
    public class WdeDropDownButton : TemplatedControl
    {
        private object? button;
        public static readonly DirectProperty<WdeDropDownButton, object?> ButtonProperty = AvaloniaProperty.RegisterDirect<WdeDropDownButton, object?>("Button", o => o.Button, (o, v) => o.Button = v);
        private bool isDropDownOpened;
        public static readonly DirectProperty<WdeDropDownButton, bool> IsDropDownOpenedProperty = AvaloniaProperty.RegisterDirect<WdeDropDownButton, bool>("IsDropDownOpened", o => o.IsDropDownOpened, (o, v) => o.IsDropDownOpened = v);

        public static readonly StyledProperty<Control?> ChildProperty =
            AvaloniaProperty.Register<WdeDropDownButton, Control?>(nameof(Child));
        
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