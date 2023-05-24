using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WDE.Common.QuickAccess;
using WDE.Module.Attributes;
using WDE.MVVM;
using WoWDatabaseEditorCore.Extensions;

namespace WoWDatabaseEditorCore.ViewModels;

[AutoRegister]
[SingleInstance]
public class TopBarQuickAccessViewModel : ObservableBase
{
    public ObservableCollection<ITopBarQuickAccessItem> Editors { get; } = new();

    public TopBarQuickAccessViewModel(IEnumerable<ITopBarQuickAccessProvider> providers)
    {
        Editors.AddRange(providers.OrderBy(x => x.Order).SelectMany(p => p.Items));
    }
}
