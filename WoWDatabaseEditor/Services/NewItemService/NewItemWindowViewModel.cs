using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using WDE.Common;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Services.NewItemService
{
    [AutoRegister]
    public class NewItemWindowViewModel : BindableBase, INewItemWindowViewModel
    {
        private NewItemPrototypeInfo? selectedPrototype;

        public NewItemWindowViewModel(IEnumerable<ISolutionItemProvider> items)
        {
            ItemPrototypes = new ObservableCollection<NewItemPrototypeInfo>();

            foreach (var item in items)
                ItemPrototypes.Add(new NewItemPrototypeInfo(item));
        }

        public ObservableCollection<NewItemPrototypeInfo> ItemPrototypes { get; }

        public NewItemPrototypeInfo? SelectedPrototype
        {
            get => selectedPrototype;
            set => SetProperty(ref selectedPrototype, value);
        }
    }
}