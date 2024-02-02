using System.Collections.Generic;
using System.Windows.Input;
using WDE.Common.Types;

namespace WDE.Common.Managers;

public interface IPopupMenuViewModel
{
    public string Title { get; }
    public IReadOnlyList<IPopupMenuItem> MenuItems { get; }
}

public interface IPopupMenuItem
{
    public ImageUri Icon { get; }
    public string Header { get; }
    public ICommand Command { get; }
    public object? CommandParameter { get; }
}