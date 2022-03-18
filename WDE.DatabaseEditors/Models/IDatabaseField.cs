﻿
using System;
using System.ComponentModel;
using WDE.Common.History;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseField : INotifyPropertyChanged, IComparable<IDatabaseField>
    {
        string FieldName { get; }
        bool IsModified { get; }
        object? OriginalValue { get; set; }
        event Action<IHistoryAction> OnChanged;
        string ToQueryString();
        IValueHolder CurrentValue { get; }
        IDatabaseField Clone();
        object? Object { get; }
        event Action<string, Action<IValueHolder>, Action<IValueHolder>> ValueChanged;
    }
}