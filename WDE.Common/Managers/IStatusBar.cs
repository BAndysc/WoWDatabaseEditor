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
    }

    public struct PlainNotification : INotification
    {
        public PlainNotification(NotificationType type, string message)
        {
            Type = type;
            Message = message;
        }

        public NotificationType Type { get; }
        public string Message { get; }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Success,
        Error
    }
}