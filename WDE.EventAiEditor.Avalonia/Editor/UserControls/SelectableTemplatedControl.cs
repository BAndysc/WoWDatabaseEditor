using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Utils;

namespace WDE.EventAiEditor.Avalonia.Editor.UserControls
{
    public abstract class SelectableTemplatedControl : TemplatedControl
    {
        public static KeyModifiers MultiselectGesture { get; } = KeyGestures.CommandModifier;
        
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
        private bool lastPressedWithControlOn = false;
        private Point pressPosition;
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            lastPressedTimestamp = e.Timestamp;
            lastClickCount = e.ClickCount;
            lastPressedWithControlOn = IsMultiSelect(e.KeyModifiers);
            pressPosition = e.GetPosition(this);

            if (e.ClickCount == 1)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                    return;

                if (!lastPressedWithControlOn)
                    DeselectAllRequest?.Execute(null);
                else if (!IsSelected)
                    DeselectOthers();    
                
                if (!lastPressedWithControlOn && !IsSelected)
                    IsSelected = true;
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
                    OnDirectEdit(tb.OverContext);
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