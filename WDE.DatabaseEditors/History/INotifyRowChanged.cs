using System;
using System.Collections.Generic;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.History
{
    public interface INotifyRowChanged
    {
        public event EventHandler<NotifyRowChangedEventArgs> OnRowChanged;
    }

    public class NotifyRowChangedEventArgs : EventArgs
    {
        public int Row { get; }
        public List<IDatabaseField>? NewValues { get; }
        public List<IDatabaseField>? OldValues { get; }

        public NotifyRowChangedEventArgs(int row, List<IDatabaseField>? newValues, List<IDatabaseField>? oldValues)
        {
            Row = row;
            NewValues = newValues;
            OldValues = oldValues;
        }
    }
}