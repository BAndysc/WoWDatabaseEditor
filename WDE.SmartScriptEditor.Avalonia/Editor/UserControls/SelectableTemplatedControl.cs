using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;

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