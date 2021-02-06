using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Services.NewItemService
{
    [AutoRegister]
    public class NewItemDialogViewModel : BindableBase, INewItemDialogViewModel
    {
        private NewItemPrototypeInfo? selectedPrototype;

        public NewItemDialogViewModel(IEnumerable<ISolutionItemProvider> items, 
            ICurrentCoreVersion coreVersion)
        {
            ItemPrototypes = new ObservableCollection<NewItemPrototypeInfo>();

            foreach (var item in items.Where(i => i.IsCompatibleWithCore(coreVersion.Current)))
                ItemPrototypes.Add(new NewItemPrototypeInfo(item));

            Accept = new DelegateCommand(() =>
            {
                CloseOk?.Invoke();
            });
        }

        public ObservableCollection<NewItemPrototypeInfo> ItemPrototypes { get; }

        public NewItemPrototypeInfo? SelectedPrototype
        {
            get => selectedPrototype;
            set => SetProperty(ref selectedPrototype, value);
        }
    
        public ICommand Accept { get; }
        public int DesiredWidth => 600;
        public int DesiredHeight => 430;
        public string Title => "New item";
        public bool Resizeable => false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}