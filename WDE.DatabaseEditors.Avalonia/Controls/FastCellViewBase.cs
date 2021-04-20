using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public abstract class FastCellViewBase : TemplatedControl
    {
        protected object cellValue = "(null)";
        public static readonly DirectProperty<FastCellViewBase, object> ValueProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, object>("Value", o => o.Value, (o, v) => o.Value = v, defaultBindingMode: BindingMode.TwoWay);
        protected string stringValue = "(null)";
        public static readonly DirectProperty<FastCellViewBase, string> StringValueProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, string>("StringValue", o => o.StringValue, (o, v) => o.StringValue = v);
        protected bool isReadOnly;
        public static readonly DirectProperty<FastCellViewBase, bool> IsReadOnlyProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, bool>("IsReadOnly", o => o.IsReadOnly, (o, v) => o.IsReadOnly = v);
        protected bool isModified;
        public static readonly DirectProperty<FastCellViewBase, bool> IsModifiedProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, bool>("IsModified", o => o.IsModified, (o, v) => o.IsModified = v);
        protected bool isActive;
        public static readonly DirectProperty<FastCellViewBase, bool> IsActiveProperty = AvaloniaProperty.RegisterDirect<FastCellViewBase, bool>("IsActive", o => o.IsActive, (o, v) => o.IsActive = v);
        
        public string StringValue
        {
            get => stringValue;
            set => SetAndRaise(StringValueProperty, ref stringValue, value);
        }

        public object Value
        {
            get => cellValue;
            set => SetAndRaise(ValueProperty, ref cellValue, value);
        }
        
        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                SetAndRaise(IsReadOnlyProperty, ref isReadOnly, value);
                UpdateOpacity();
            }
        }

        private void UpdateOpacity()
        {
            Opacity = isActive ? (isReadOnly ? 0.5f : 1f) : 0f;
        }

        public bool IsModified
        {
            get => isModified;
            set
            {
                SetAndRaise(IsModifiedProperty, ref isModified, value);
                this.FontWeight = isModified ? FontWeight.Bold : FontWeight.Normal;
            }
        }

        public bool IsActive
        {
            get => isActive;
            set
            {
                SetAndRaise(IsActiveProperty, ref isActive, value);
                IsHitTestVisible = value;
                UpdateOpacity();
            }
        }
        
        static FastCellViewBase()
        {
            AffectsRender<FastCellViewBase>(IsModifiedProperty);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (isModified)
            {
                context.DrawRectangle(Brushes.Red, null, new Rect(0, 6, 12, 12));
            }
        }
    }
}