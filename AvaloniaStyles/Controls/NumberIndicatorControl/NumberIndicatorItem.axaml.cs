using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaloniaStyles.Controls
{
    public class NumberIndicatorItem : TemplatedControl
    {
        private uint number;
        public static readonly DirectProperty<NumberIndicatorItem, uint> NumberProperty = AvaloniaProperty.RegisterDirect<NumberIndicatorItem, uint>("Number", o => o.Number, (o, v) => o.Number = v);
        
        private bool isActive;
        public static readonly DirectProperty<NumberIndicatorItem, bool> IsActiveProperty = AvaloniaProperty.RegisterDirect<NumberIndicatorItem, bool>("IsActive", o => o.IsActive, (o, v) => o.IsActive = v);

        public uint Number
        {
            get => number;
            set => SetAndRaise(NumberProperty, ref number, value);
        }

        public bool IsActive
        {
            get => isActive;
            set => SetAndRaise(IsActiveProperty, ref isActive, value);
        }

        static NumberIndicatorItem()
        {
            IsActiveProperty.Changed.AddClassHandler<NumberIndicatorItem>((o, e) =>
            {
                o.PseudoClasses.Set(":active", o.isActive);
            });
        }
    }
}