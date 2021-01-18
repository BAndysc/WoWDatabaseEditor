namespace WDE.Common.Managers
{
    public interface IDialog
    {
        int DesiredWidth { get; }
        int DesiredHeight { get; }
        string Title { get; }
        bool Resizeable { get; }

        event System.Action CloseCancel;
        event System.Action CloseOk;
    }
}