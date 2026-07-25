using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Avalonia.DnD;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Editor.ViewModels;

namespace WDE.DbScriptsEditor.Avalonia.Editor.UserControls
{
    // Copy-adapted from WDE.EventAiEditor.Avalonia SelectableTemplatedControl so every dbscript
    // row (action, wait, comment — equal entities) renders and behaves exactly like the
    // EventAI/SmartScript action list:
    //  - single click selects the row (Ctrl toggles multi-select, Shift range-selects),
    //  - single click on an inline [p=N] parameter link edits that parameter in place,
    //  - double click opens the row's editor,
    //  - dragging moves the row (or the whole selection when the row is part of it).
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

        // Shift-click: select the range from the current anchor to this item.
        public static readonly AvaloniaProperty RangeSelectRequestProperty =
            AvaloniaProperty.Register<SelectableTemplatedControl, ICommand>(nameof(RangeSelectRequest));

        public ICommand RangeSelectRequest
        {
            get => (ICommand?) GetValue(RangeSelectRequestProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(RangeSelectRequestProperty, value);
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
        // Pressing an already-selected row must NOT collapse the multi-selection immediately —
        // the press may be the start of dragging the whole selection. The collapse happens on
        // release instead, and only if the pointer didn't move (i.e. it stayed a click).
        private bool deferredCollapseToThis;
        private Point pressPosition;

        // Drag-to-reorder (shared by all row kinds — action, wait, comment are equal entities).
        private bool dragArmed;
        private PointerPressedEventArgs? pressArgs;

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            // Right-click: select the row (if not already part of the selection) and let the
            // event through so the context menu opens for it.
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (!IsSelected)
                {
                    DeselectAllRequest?.Execute(null);
                    IsSelected = true;
                }
                return;
            }

            lastPressedTimestamp = e.Timestamp;
            lastClickCount = e.ClickCount;
            lastPressedWithControlOn = IsMultiSelect(e.KeyModifiers);
            deferredCollapseToThis = false;
            pressPosition = e.GetPosition(this);

            // Only a plain left press (not on an inline link — that's a value edit) arms a drag.
            dragArmed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
                        !(e.Source is FormattedTextBlock linkTb && linkTb.OverContext != null);
            pressArgs = e;

            if (e.ClickCount == 1)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                    return;

                if (e.KeyModifiers.HasFlagFast(KeyModifiers.Shift))
                {
                    RangeSelectRequest?.Execute(DataContext);
                    e.Handled = true;
                    return;
                }

                if (!lastPressedWithControlOn)
                {
                    if (!IsSelected)
                    {
                        DeselectAllRequest?.Execute(null);
                        IsSelected = true;
                    }
                    else
                        deferredCollapseToThis = true;
                }
                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!dragArmed)
                return;
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                dragArmed = false;
                return;
            }
            var pos = e.GetPosition(this);
            if (Math.Abs(pos.Y - pressPosition.Y) < 5 && Math.Abs(pos.X - pressPosition.X) < 5)
                return;

            dragArmed = false;
            if (DataContext is not DbScriptRowViewModel ctx || pressArgs is not { } press)
                return;
            _ = DragDrop.DoDragDropAsync(press, IDataTransfer.Create(ctx), DragDropEffects.Move);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            dragArmed = false;
            if (e.InitialPressMouseButton != MouseButton.Left)
                return;
            if (lastClickCount == 1 && (e.Timestamp - lastPressedTimestamp) <= 1000)
            {
                if (e.Source is FormattedTextBlock tb && tb.OverContext != null)
                {
                    OnDirectEdit(tb.OverContext);
                    e.Handled = true;
                }
                else
                {
                    var vector = pressPosition - e.GetPosition(this);
                    var dist = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
                    if (dist < 5)
                    {
                        if (lastPressedWithControlOn)
                        {
                            // Ctrl/Cmd-click toggles this row, keeping the rest of the selection.
                            IsSelected = !IsSelected;
                            e.Handled = true;
                        }
                        else if (deferredCollapseToThis)
                        {
                            // A plain click (not a drag) on a selected row collapses to just it.
                            DeselectAllRequest?.Execute(null);
                            IsSelected = true;
                            e.Handled = true;
                        }
                    }
                }
            }
            deferredCollapseToThis = false;
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
