using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.ItemFromListSelectorService
{
    [AutoRegister]
    public class ItemFromListProvider : IItemFromListProvider
    {
        private readonly IWindowManager windowManager;

        public ItemFromListProvider(IWindowManager windowManager)
        {
            this.windowManager = windowManager;
        }
        
        public async Task<long?> GetItemFromList(Dictionary<long, SelectOption>? items, bool flags, long? current = null, string? title = null)
        {
            using LongItemFromListProviderViewModel vm = new(items, flags, current, title);
            if (await windowManager.ShowDialog(vm))
                return vm.GetEntry();
            return null;
        }
        
        public async Task<string?> GetItemFromList(Dictionary<string, SelectOption>? items, bool multiSelect, string? current = null, string? title = null)
        {
            using StringItemFromListProviderViewModel vm = new(items, multiSelect, current, title);
            if (await windowManager.ShowDialog(vm))
                return vm.GetEntry();
            return null;
        }
        
        public async Task<float?> GetItemFromList(Dictionary<float, SelectOption>? items, string? title = null)
        {
            using FloatItemFromListProviderViewModel vm = new(items, 0, title);
            if (await windowManager.ShowDialog(vm))
                return vm.GetEntry();
            return null;
        }
    }
}