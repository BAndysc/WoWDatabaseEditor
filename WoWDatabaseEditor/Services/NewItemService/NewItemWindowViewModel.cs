using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Practices.Unity;
using WDE.Common;
using WDE.SmartScriptEditor;

namespace WoWDatabaseEditor.Services.NewItemService
{
    public class NewItemWindowViewModel : BindableBase, INewItemWindowViewModel
    {
        private IUnityContainer container;

        public NewItemWindowViewModel(IUnityContainer container)
        {
            this.container = container;
            ItemPrototypes = new ObservableCollection<NewItemPrototypeInfo>();
            var items = container.ResolveAll<ISolutionItemProvider>();

            foreach (var item in items)
            {
                ItemPrototypes.Add(new NewItemPrototypeInfo(item));
            }
        }
        
        public ObservableCollection<NewItemPrototypeInfo> ItemPrototypes { get; set; }

        private NewItemPrototypeInfo _selectedPrototype;
        public NewItemPrototypeInfo SelectedPrototype
        {
            get { return _selectedPrototype; }
            set { SetProperty(ref _selectedPrototype, value); }
        }
    }
}
