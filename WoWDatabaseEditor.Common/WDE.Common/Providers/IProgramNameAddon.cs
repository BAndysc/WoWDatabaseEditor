using System.ComponentModel;
using WDE.Module.Attributes;

namespace WDE.Common.Providers;

[NonUniqueProvider]
public interface IProgramNameAddon : INotifyPropertyChanged
{
    string Addon { get; }
}