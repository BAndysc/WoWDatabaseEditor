using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WDE.Common.Parameters;
using WDE.Common.Providers;
using Prism.Ioc;

namespace WoWDatabaseEditor.Services.ItemFromListSelectorService
{
    [WDE.Common.Attributes.AutoRegister]
    public class ItemFromListProvider : IItemFromListProvider
    {
        public ItemFromListProvider()
        {
        }

        public int? GetItemFromList(Dictionary<int, SelectOption> items, bool flags)
        {
            ItemFromListProviderView view = new ItemFromListProviderView();
            ItemFromListProviderViewModel vm = new ItemFromListProviderViewModel(items, flags);
            view.DataContext = vm;
            if (view.ShowDialog().Value)
                return vm.GetEntry();
            return null;
        }
    }
}
