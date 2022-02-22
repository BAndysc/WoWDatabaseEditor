using System.Windows.Input;

namespace WDE.Common.Managers
{
    public interface IDialog
    {
        int DesiredWidth { get; }
        int DesiredHeight { get; }
        string Title { get; }
        bool Resizeable { get; }

        ICommand Accept { get; }
        ICommand Cancel { get; }
        event System.Action CloseCancel;
        event System.Action CloseOk;
    }

    public interface IClosableDialog
    {
        void OnClose();
    }
}