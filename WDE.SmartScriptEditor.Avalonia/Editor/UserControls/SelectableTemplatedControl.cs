using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;

namespace WDE.SmartScriptEditor.Avalonia.Editor.UserControls
{
    public abstract class SelectableTemplatedControl : TemplatedControl
    {
        public static KeyModifiers MultiselectGesture { get; } = KeyGestures.CommandModifier;
        
        public static readonly StyledProperty<IBrush> SpecialBackgroundProperty = AvaloniaProperty.Register<SelectableTemplatedControl, IBrush>(nameof(SpecialBackground));

        public IBrush SpecialBackground
        {
            get => (IBrush)GetValue(SpecialBackgroundProperty);
            set => SetValue(SpecialBackgroundProperty, value);
        }

        public static readonly AvaloniaProperty DeselectAllRequestProperty =
            AvaloniaProperty.Register<SelectableTemplatedControl, ICommand>(nameof(DeselectAllRequest));

        public ICommand DeselectAllRequest
        {
            get => (ICommand?) GetValue(DeselectAllRequestProperty) ?? AlwaysDisabledCommand.Command; 
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
        private bool wasAlreadySelectedOnLastPressed;
        private bool lastPressedWithControlOn = false;
        private bool lastPressedWithShiftOn = false;
        private Point pressPosition;
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            lastPressedTimestamp = e.Timestamp;
            lastClickCount = e.ClickCount;
            wasAlreadySelectedOnLastPressed = isSelected;
            lastPressedWithControlOn = IsMultiSelect(e.KeyModifiers);
            lastPressedWithShiftOn = e.KeyModifiers.HasFlagFast(KeyModifiers.Shift);
            pressPosition = e.GetPosition(this);

            if (e.ClickCount == 1)
            {
                // in the past pressing on a link text wasn't selecting the item
                // but this yield problems with dragging, when the drag is initialized by clicking on a link
                //if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                //    return;

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
            var releasePosition = e.GetPosition(this);
            var vector = pressPosition - releasePosition;
            var dist = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
            var hasDragged = dist >= 5;
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
                        if (!hasDragged)
                        {
                            DeselectOthers();
                            IsSelected = !IsSelected;
                            e.Handled = true;   
                        }
                    }
                    else if (wasAlreadySelectedOnLastPressed && !hasDragged)
                    {
                        DeselectAllRequest?.Execute(null);
                        IsSelected = true;
                        e.Handled = true;
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
