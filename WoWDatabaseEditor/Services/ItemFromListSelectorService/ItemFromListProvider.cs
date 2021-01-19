using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Services.ItemFromListSelectorService
{
    [AutoRegister]
    public class ItemFromListProvider : IItemFromListProvider
    {
        private readonly IWindowManager windowManager;

        public ItemFromListProvider(IWindowManager windowManager)
        {
            this.windowManager = windowManager;
        }
        
        public int? GetItemFromList(Dictionary<int, SelectOption> items, bool flags, int? current = null)
        {
            ItemFromListProviderViewModel vm = new(items, flags, current);
            if (windowManager.ShowDialog(vm))
                return vm.GetEntry();
            return null;
        }
    }
}