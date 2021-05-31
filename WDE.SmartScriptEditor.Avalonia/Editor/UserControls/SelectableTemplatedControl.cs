using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using WDE.Common.Avalonia.Controls;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    public class SelectableTemplatedControl : TemplatedControl
    {
        public static KeyModifiers MultiselectGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;
        
        public static readonly DirectProperty<SelectableTemplatedControl, bool> IsSelectedProperty =
            AvaloniaProperty.RegisterDirect<SelectableTemplatedControl, bool>(
                nameof(IsSelected),
                o => o.IsSelected,
                (o, v) => o.IsSelected = v);
        
        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetAndRaise(IsSelectedProperty, ref isSelected, value);
        }

        private ulong lastPressedTimestamp = 0;
        private int lastClickCount = 0;
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            lastPressedTimestamp = e.Timestamp;
            lastClickCount = e.ClickCount;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (lastClickCount == 1 && (e.Timestamp - lastPressedTimestamp) <= 1000)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                {
                    OnDirectEdit(tb.OverContext);
                    e.Handled = true;
                }
            }
            if (lastClickCount == 2 && (e.Timestamp - lastPressedTimestamp) <= 1000)
            {
                OnEdit();
                e.Handled = true;
            }
        }
        
        protected virtual void OnEdit() {}
        protected virtual void OnDirectEdit(object context) {}
        
        static SelectableTemplatedControl()
        {
            IsSelectedProperty.Changed.AddClassHandler<SelectableTemplatedControl>((control, args) =>
            {
                if (args.NewValue is bool b)
                    control.PseudoClasses.Set(":selected", b);
            });
        }
    }
}