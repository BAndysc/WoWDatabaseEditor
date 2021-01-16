namespace WDE.Common.Services.MessageBox
{
    public interface IMessageBoxService
    {
        T ShowDialog<T>(IMessageBox<T> messageBox);
    }
}