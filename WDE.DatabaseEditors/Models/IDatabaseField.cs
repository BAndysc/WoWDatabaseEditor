
using System;
using System.ComponentModel;
using WDE.Common.History;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseField : INotifyPropertyChanged
    {
        string FieldName { get; }
        bool IsModified { get; }
        object? OriginalValue { get; set; }
        event Action<IHistoryAction> OnChanged;
        string ToQueryString();
        IDatabaseField Clone();
    }
}