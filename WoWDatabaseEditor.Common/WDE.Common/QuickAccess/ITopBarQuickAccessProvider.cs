using System.Collections.Generic;
using System.Windows.Input;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common.QuickAccess;

[NonUniqueProvider]
public interface ITopBarQuickAccessProvider
{
    IEnumerable<ITopBarQuickAccessItem> Items { get; }
    int Order { get; }
}

public interface ITopBarQuickAccessItem
{
    ICommand Command { get; }
    string Name { get; }
    ImageUri Icon { get; }
}
