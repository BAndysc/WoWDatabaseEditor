using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using WDE.Common.Parameters;
using WDE.Common.Providers;

namespace WoWDatabaseEditor.Services.ItemFromListSelectorService
{
    public class ItemFromListProvider : IItemFromListProvider
    {
        private IUnityContainer _container;

        public ItemFromListProvider(IUnityContainer container)
        {
            _container = container;
        }

        public int? GetItemFromList(Dictionary<int, SelectOption> items, bool flags)
        {
            ItemFromListProviderView view = new ItemFromListProviderView();
            ItemFromListProviderViewModel vm = new ItemFromListProviderViewModel(_container, items, flags);
            view.DataContext = vm;
            if (view.ShowDialog().Value)
                return vm.GetEntry();
            return null;
        }
    }
}
