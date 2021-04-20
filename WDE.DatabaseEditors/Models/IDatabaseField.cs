
using System;
using System.ComponentModel;

namespace WDE.DatabaseEditors.Models
{
    public interface IDatabaseField : INotifyPropertyChanged
    {
        bool IsModified { get; }
    }
}