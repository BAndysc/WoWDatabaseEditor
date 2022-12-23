using System.ComponentModel;
using WDE.Module.Attributes;

namespace WDE.Common.Outliner;

[UniqueProvider]
public interface IOutlinerToolService : INotifyPropertyChanged
{
    IOutlinerItemViewModel? SelectedItem { get; }
}