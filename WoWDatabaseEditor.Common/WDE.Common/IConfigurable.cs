using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [NonUniqueProvider]
    public interface IConfigurable : INotifyPropertyChanged
    {
        ICommand Save { get; }
        string Name { get; }
        bool IsModified { get; }
        bool IsRestartRequired { get; }
    }
}