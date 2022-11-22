using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using WDE.Common.Avalonia.Controls;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    public abstract class SelectableTemplatedControl : TemplatedControl
    {
        public static KeyModifiers MultiselectGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.CommandModifiers ?? KeyModifiers.Control;
        
        public static readonly AvaloniaProperty DeselectAllRequestProperty =
            AvaloniaProperty.Register<SelectableTemplatedControl, ICommand>(nameof(DeselectAllRequest));

        public ICommand DeselectAllRequest
        {
            get => (ICommand) GetValue(DeselectAllRequestProperty);
            set => SetValue(DeselectAllRequestProperty, value);
        }
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

        private bool IsMultiSelect(KeyModifiers modifiers)
        {
            return modifiers.HasFlagFast(MultiselectGesture);
        }

        private ulong lastPressedTimestamp = 0;
        private int lastClickCount = 0;
        private bool lastPressedWithControlOn = false;
        private bool lastPressedWithShiftOn = false;
        private Point pressPosition;
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            lastPressedTimestamp = e.Timestamp;
            lastClickCount = e.ClickCount;
            lastPressedWithControlOn = IsMultiSelect(e.KeyModifiers);
            lastPressedWithShiftOn = e.KeyModifiers.HasFlagFast(KeyModifiers.Shift);
            pressPosition = e.GetPosition(this);

            if (e.ClickCount == 1)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                    return;

                if (IsSelected)
                {
                    DeselectOthers();
                    IsSelected = true;
                }
                else
                {
                    if (!lastPressedWithControlOn)
                        DeselectAllRequest?.Execute(null);
                    else if (!IsSelected)
                        DeselectOthers();
                
                    if (!lastPressedWithControlOn && !IsSelected)
                        IsSelected = true;
                }
                e.Handled = true;
            }
        }

        protected abstract void DeselectOthers();

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (lastClickCount == 1 && (e.Timestamp - lastPressedTimestamp) <= 1000)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                {
                    OnDirectEdit(e.KeyModifiers.HasFlagFast(MultiselectGesture), tb.OverContext);
                    e.Handled = true;
                }
                else
                {
                    if (lastPressedWithControlOn)
                    {
                        var vector = pressPosition - e.GetPosition(this);
                        var dist = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
                        if (dist < 5)
                        {
                            DeselectOthers();
                            IsSelected = !IsSelected;
                            e.Handled = true;   
                        }
                    }
                }
            }
            if (lastClickCount == 2 && (e.Timestamp - lastPressedTimestamp) <= 1000)
            {
                OnEdit();
                e.Handled = true;
            }
        }
        
        protected virtual void OnEdit() {}
        protected virtual void OnDirectEdit(bool controlPressed, object context) {}
        
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