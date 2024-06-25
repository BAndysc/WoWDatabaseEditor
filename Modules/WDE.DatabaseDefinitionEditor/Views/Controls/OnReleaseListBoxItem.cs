using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace WDE.DatabaseDefinitionEditor.Views.Controls;

// this changes the listbox to update the selection on pointer release rather than pointer press (originally handled in ListBoxItem)
public class OnReleaseListBox : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);

    static OnReleaseListBox()
    {
        PointerPressedEvent.AddClassHandler<OnReleaseListBox>((x, e) => x.OnPointerPressed(e), RoutingStrategies.Tunnel);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerReleased(Avalonia.Input.PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Source is Visual source)
        {
            if (source.FindAncestorOfType<ScrollBar>() != null)
                return;

            var point = e.GetCurrentPoint(source);

            if (point.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonReleased or PointerUpdateKind.RightButtonReleased)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    e.KeyModifiers.HasFlagFast(KeyModifiers.Shift),
                    e.KeyModifiers.HasFlagFast(KeyModifiers.Control),
                    point.Properties.IsRightButtonPressed);
            }
        }
    }
}