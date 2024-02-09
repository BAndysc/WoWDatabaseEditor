using System.ComponentModel;
using System.Windows.Input;

namespace WDE.Common.Services;

public interface IUpdateViewModel : INotifyPropertyChanged
{
    bool HasPendingUpdate { get; }
    ICommand InstallPendingCommandUpdate { get; }
}