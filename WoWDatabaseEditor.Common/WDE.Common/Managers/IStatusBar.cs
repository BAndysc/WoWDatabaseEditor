﻿using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.Utils;
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
        ICommand? ClickCommand { get; }
    }

    public struct PlainNotification : INotification
    {
        public PlainNotification(NotificationType type, string message, ICommand? clickCommand = null)
        {
            Type = type;
            Message = message;
            ClickCommand = clickCommand;
        }

        public NotificationType Type { get; }
        public string Message { get; }
        public ICommand? ClickCommand { get; }
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Success,
        Error
    }
    
    // due to lack of dynamic status bar items, this is a workaround for now (to predefine the type)
    [UniqueProvider]
    public interface IConnectionsStatusBarItem : INotifyPropertyChanged
    {
        int OpenedConnections { get; }
        bool IsPanelVisible { get; set; }
    }
    
    [FallbackAutoRegister]
    internal class NullConnectionsStatusBarItem : IConnectionsStatusBarItem
    {
        public int OpenedConnections => 0;
        public bool IsPanelVisible { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}