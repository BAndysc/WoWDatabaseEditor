using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WDE.Common.QuickAccess;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.ViewModels;

[AutoRegister]
[SingleInstance]
public class TopBarQuickAccessViewModel : ObservableBase
{
    public ObservableCollection<ITopBarQuickAccessItem> Editors { get; } = new();

    public TopBarQuickAccessViewModel(IEnumerable<ITopBarQuickAccessProvider> providers)
    {
        Editors.AddRange(providers.SelectMany(p => p.Items));
    }
}
