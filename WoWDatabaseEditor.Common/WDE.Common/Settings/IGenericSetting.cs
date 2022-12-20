using System.ComponentModel;

namespace WDE.Common.Settings;

public interface IGenericSetting : INotifyPropertyChanged
{
    string Name { get; }
    string? Help { get; }
}