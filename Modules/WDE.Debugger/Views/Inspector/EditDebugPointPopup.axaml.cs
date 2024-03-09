using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Prism.Commands;

namespace WDE.Debugger.Views.Inspector;

internal partial class EditDebugPointPopup : UserControl
{
    private Popup? popup;
    private bool resultSet;

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    private TaskCompletionSource<bool> tcs;

    public EditDebugPointPopup()
    {
        tcs = new TaskCompletionSource<bool>();
        OkCommand = new DelegateCommand(() =>
        {
            if (resultSet)
                return;

            resultSet = true;
            tcs.SetResult(true);
            popup?.Close();
        });
        CancelCommand = new DelegateCommand(() =>
        {
            if (resultSet)
                return;

            resultSet = true;
            tcs.SetResult(false);
            popup?.Close();
        });
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async Task<bool> Open(Control control)
    {
        tcs = new TaskCompletionSource<bool>();
        resultSet = false;

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

        return await tcs.Task;
    }

    private void PopupClosed(object? sender, EventArgs e)
    {
        if (!resultSet)
        {
            resultSet = true;
            tcs.SetResult(true);
        }
        ((ISetLogicalParent)popup!).SetParent(null);
    }
}