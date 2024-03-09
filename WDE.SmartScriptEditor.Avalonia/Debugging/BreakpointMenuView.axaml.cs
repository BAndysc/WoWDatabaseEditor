using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace WDE.SmartScriptEditor.Avalonia.Debugging;

public partial class BreakpointMenuView : UserControl
{
    private Popup? popup;

    public BreakpointMenuView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Close()
    {
        if (popup != null)
        {
            popup.Close();
        }
    }

    public void Open(Control control)
    {
        if (popup == null)
        {
            popup = new Popup()
            {
                PlacementTarget = control,
                Placement = PlacementMode.Pointer,
                IsLightDismissEnabled = true,
                OverlayDismissEventPassThrough = true,
                Child = this
            };
            popup.Closed += PopupClosed;
        }
        if (!ReferenceEquals(popup.Parent, control))
        {
            ((ISetLogicalParent)popup).SetParent(null);
            ((ISetLogicalParent)popup).SetParent(control);
        }
        popup.Open();
    }

    private void PopupClosed(object? sender, EventArgs e)
    {
        ((ISetLogicalParent)popup!).SetParent(null);
    }
}

public class BreakpointContextMenu : ContextMenu
{
    protected override Type StyleKeyOverride => typeof(ContextMenu);

    public override void Close()
    {
        base.Close();
        this.FindAncestorOfType<BreakpointMenuView>()?.Close();
    }
}