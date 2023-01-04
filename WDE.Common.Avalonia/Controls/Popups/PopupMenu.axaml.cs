using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace WDE.Common.Avalonia.Controls.Popups;

public partial class PopupMenu : UserControl
{
    private Popup? popup;
    private TaskCompletionSource? popupClosed;

    public PopupMenu()
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
            popup.PlacementTarget = null;
            DataContext = null;
        }
    }

    public async Task Open(Control control)
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
        else
        {
            popup.PlacementTarget = control;
        }
        if (!ReferenceEquals(popup.Parent, control))
        {
            ((ISetLogicalParent)popup).SetParent(null);
            ((ISetLogicalParent)popup).SetParent(control);
        }

        popupClosed = new TaskCompletionSource();
        popup.Open();
        await popupClosed.Task;
    }

    private void PopupClosed(object? sender, EventArgs e)
    {
        ((ISetLogicalParent)popup!).SetParent(null);
        popupClosed?.SetResult();
    }
}