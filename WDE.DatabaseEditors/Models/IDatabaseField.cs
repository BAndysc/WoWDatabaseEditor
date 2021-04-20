
using System;
using System.ComponentModel;
using WDE.Common.History;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseField : INotifyPropertyChanged
    {
        bool IsModified { get; }
        event Action<IHistoryAction> OnChanged;
    }
}