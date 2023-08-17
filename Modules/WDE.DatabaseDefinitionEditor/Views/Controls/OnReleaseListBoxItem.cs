using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace WDE.DatabaseDefinitionEditor.Views.Controls;

// note: in avalonia 11 this is not required.
// this changes the listbox to update the selection on pointer release rather than pointer press
public class OnReleaseListBox : ListBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(ListBox);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerReleased(Avalonia.Input.PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Source is IVisual source)
        {
            var point = e.GetCurrentPoint(source);

            if (point.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonReleased or PointerUpdateKind.RightButtonReleased)
            {
                e.Handled = UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Control),
                    point.Properties.IsRightButtonPressed);
            }
        }
    }
}