using System.Collections.Generic;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Services.ItemFromListSelectorService
{
    [AutoRegister]
    public class ItemFromListProvider : IItemFromListProvider
    {
        public int? GetItemFromList(Dictionary<int, SelectOption> items, bool flags)
        {
            ItemFromListProviderView view = new();
            ItemFromListProviderViewModel vm = new(items, flags);
            view.DataContext = vm;
            if (view.ShowDialog() ?? false)
                return vm.GetEntry();
            return null;
        }
    }
}