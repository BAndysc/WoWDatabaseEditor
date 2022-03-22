using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    [AutoRegister]
    public class NewItemDialogViewModel : BindableBase, INewItemDialogViewModel
    {
        private string customName = "New folder";
        private NewItemPrototypeInfo? selectedPrototype;
        private NewItemPrototypeGroup? selectedCategory;

        public NewItemDialogViewModel(ISolutionItemProvideService provider, ICurrentCoreVersion currentCore)
        {
            Dictionary<string, NewItemPrototypeGroup> groups = new();
            Categories = new ObservableCollection<NewItemPrototypeGroup>();

            bool coreIsSpecific = currentCore.IsSpecified;
            foreach (var item in provider.All)
            {
                if (!groups.TryGetValue(item.GetGroupName(), out var group))
                {
                    group = new NewItemPrototypeGroup(item.GetGroupName());
                    groups[item.GetGroupName()] = group;
                    Categories.Add(group);
                }

                bool isCompatible = item.IsCompatibleWithCore(currentCore.Current);
                
                if (!isCompatible && coreIsSpecific)
                    continue;

                var info = new NewItemPrototypeInfo(item, isCompatible);
                group.Add(info);
            }

            Accept = new DelegateCommand(() =>
            {
                CloseOk?.Invoke();
            });
            Cancel = new DelegateCommand(() =>
            {
                CloseCancel?.Invoke();
            });

            Categories.RemoveIf(c => c.Count == 0);
            
            if (Categories.Count > 0)
                SelectedCategory = Categories[0];
            
            if (Categories.Count > 0 && Categories[0].Count > 0)
                SelectedPrototype = Categories[0][0];
        }

        public void AllowFolders(bool showFolders)
        {
            if (!showFolders)
            {
                foreach (var group in Categories)
                    group.Remove(group.Where(i => i.IsContainer).ToList());
                Categories.Remove(Categories.Where(i => i.Count == 0).ToList());

                if (Categories.Count > 0 && Categories[0].Count > 0)
                    SelectedPrototype = Categories[0][0];
            }
        }
        
        public ObservableCollection<NewItemPrototypeGroup> Categories { get; }

        public NewItemPrototypeGroup? SelectedCategory
        {
            get => selectedCategory;
            set => SetProperty(ref selectedCategory, value);
        }

        public NewItemPrototypeInfo? SelectedPrototype
        {
            get => selectedPrototype;
            set => SetProperty(ref selectedPrototype, value);
        }
        
        public string CustomName
        {
            get => customName;
            set => SetProperty(ref customName, value);
        }

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            if (SelectedPrototype == null)
                return null;

            return await SelectedPrototype.CreateSolutionItem(CustomName);
        }

        public ICommand Cancel { get; }
        public ICommand Accept { get; }
        public int DesiredWidth => 700;
        public int DesiredHeight => 580;
        public string Title => "New item";
        public bool Resizeable => true;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class NewItemPrototypeGroup : ObservableCollection<NewItemPrototypeInfo>, INotifyPropertyChanged
    {
        public NewItemPrototypeGroup(string groupName)
        {
            GroupName = groupName;
        }
        
        public string GroupName { get; }
    }
}