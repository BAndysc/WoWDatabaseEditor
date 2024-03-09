using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace LoaderAvalonia.Web;

[AutoRegister]
[SingleInstance]
public class NullUpdateViewModel : IUpdateViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool HasPendingUpdate => false;
    public ICommand InstallPendingCommandUpdate { get; } = AlwaysDisabledCommand.Command;
}