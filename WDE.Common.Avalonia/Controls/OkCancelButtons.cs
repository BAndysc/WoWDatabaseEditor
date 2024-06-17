using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace WDE.Common.Avalonia.Controls;

public class OkCancelButtons : TemplatedControl
{
    private ICommand? acceptCommand;
    private ICommand? cancelCommand;

    public static readonly DirectProperty<OkCancelButtons, ICommand?> AcceptCommandProperty = AvaloniaProperty.RegisterDirect<OkCancelButtons, ICommand?>("AcceptCommand", o => o.AcceptCommand, (o, v) => o.AcceptCommand = v);
    public static readonly DirectProperty<OkCancelButtons, ICommand?> CancelCommandProperty = AvaloniaProperty.RegisterDirect<OkCancelButtons, ICommand?>("CancelCommand", o => o.CancelCommand, (o, v) => o.CancelCommand = v);
    public static readonly StyledProperty<string> AcceptTextProperty = AvaloniaProperty.Register<OkCancelButtons, string>("AcceptText", "Accept");
    public static readonly StyledProperty<bool> DisplayCancelButtonProperty = AvaloniaProperty.Register<OkCancelButtons, bool>("Cancel", true);

    public ICommand? AcceptCommand
    {
        get => acceptCommand;
        set => SetAndRaise(AcceptCommandProperty, ref acceptCommand, value);
    }

    public ICommand? CancelCommand
    {
        get => cancelCommand;
        set => SetAndRaise(CancelCommandProperty, ref cancelCommand, value);
    }

    public string AcceptText
    {
        get { return (string)GetValue(AcceptTextProperty); }
        set { SetValue(AcceptTextProperty, value); }
    }

    public bool DisplayCancelButton
    {
        get { return (bool)GetValue(DisplayCancelButtonProperty); }
        set { SetValue(DisplayCancelButtonProperty, value); }
    }
}