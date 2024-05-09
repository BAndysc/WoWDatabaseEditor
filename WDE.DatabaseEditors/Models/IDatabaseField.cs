
using System;
using System.ComponentModel;
using WDE.Common.History;
using WDE.DatabaseEditors.Data.Structs;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseField : INotifyPropertyChanged, IComparable<IDatabaseField>
    {
        ColumnFullName FieldName { get; }
        bool IsModified { get; }
        object? OriginalValue { get; set; }
        event Action<IHistoryAction> OnChanged;
        string ToQueryString();
        IValueHolder CurrentValue { get; }
        IDatabaseField Clone();
        object? Object { get; }
        event Action<ColumnFullName, Action<IValueHolder>, Action<IValueHolder>> ValueChanged;
    }
}