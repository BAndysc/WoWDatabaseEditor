using System.ComponentModel;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ITablesToolService : INotifyPropertyChanged
{
    bool Visibility { get; }
    void Open();
    void Close();
}