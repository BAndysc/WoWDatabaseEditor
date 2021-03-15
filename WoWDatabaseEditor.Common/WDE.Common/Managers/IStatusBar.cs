using System.Windows.Input;
using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IStatusBar
    {
        void PublishNotification(INotification notification);
    }

    public interface INotification
    {
        NotificationType Type { get; }
        string Message { get; }
        ICommand ClickCommand { get; }
    }

    public struct PlainNotification : INotification
    {
        public PlainNotification(NotificationType type, string message, ICommand clickCommand = null)
        {
            Type = type;
            Message = message;
            ClickCommand = clickCommand;
        }

        public NotificationType Type { get; }
        public string Message { get; }
        public ICommand ClickCommand { get; }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Success,
        Error
    }
}