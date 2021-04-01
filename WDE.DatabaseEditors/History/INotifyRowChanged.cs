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
        public List<IDbTableField>? NewValues { get; }
        public List<IDbTableField>? OldValues { get; }

        public NotifyRowChangedEventArgs(int row, List<IDbTableField>? newValues, List<IDbTableField>? oldValues)
        {
            Row = row;
            NewValues = newValues;
            OldValues = oldValues;
        }
    }
}