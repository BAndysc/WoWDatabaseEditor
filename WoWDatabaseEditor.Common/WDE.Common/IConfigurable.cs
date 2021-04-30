using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.Annotations;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [NonUniqueProvider]
    public interface IConfigurable : INotifyPropertyChanged
    {
        ICommand Save { get; }
        string Name { get; }
        [CanBeNull] string ShortDescription { get; }
        bool IsModified { get; }
        bool IsRestartRequired { get; }
        
        void ConfigurableOpened() { }
    }
}