using System.Windows.Input;
using WDE.Common.Types;

namespace WDE.Common.Managers
{
    public interface IDialogWindowBase
    {
        int DesiredWidth { get; }
        int DesiredHeight { get; }
        string Title { get; }
        bool Resizeable { get; }
        bool AutoSize => false;
        void OnWindowOpened() { }
    }
    
    public interface IDialog : IDialogWindowBase
    {
        ICommand Accept { get; }
        ICommand Cancel { get; }
        event System.Action CloseCancel;
        event System.Action CloseOk;
    }

    public interface IWindowViewModel : IDialogWindowBase
    {
        ImageUri? Icon { get; }
    }

    public interface IClosableDialog
    {
        void OnClose();
        event System.Action Close;
    }
}