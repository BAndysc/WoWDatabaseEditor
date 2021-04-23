using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Solution;

namespace WDE.Solutions.Explorer.ViewModels
{
    public class SolutionItemViewModel : BindableBase
    {
        private readonly ISolutionItemNameRegistry itemNameRegistry;

        private readonly Dictionary<ISolutionItem, SolutionItemViewModel> itemToViewmodel;
        private bool isExpanded;

        private bool isSelected;

        public SolutionItemViewModel(ISolutionItemNameRegistry itemNameRegistry, ISolutionItem item) : this(itemNameRegistry,
            item,
            null)
        {
        }

        public SolutionItemViewModel(ISolutionItemNameRegistry itemNameRegistry, ISolutionItem item, SolutionItemViewModel parent)
        {
            this.itemNameRegistry = itemNameRegistry;
            Item = item;
            Parent = parent;

            itemToViewmodel = new Dictionary<ISolutionItem, SolutionItemViewModel>();

            if (item.Items != null)
            {
                Children = new ObservableCollection<SolutionItemViewModel>();

                foreach (object obj in item.Items)
                    AddItem(obj as ISolutionItem);

                item.Items.CollectionChanged += (sender, args) =>
                {
                    if (args.NewItems != null)
                    {
                        var i = 0;
                        foreach (object obj in args.NewItems)
                            AddItem(obj as ISolutionItem, args.NewStartingIndex + i);
                    }

                    if (args.OldItems != null)
                    {
                        foreach (object obj in args.OldItems)
                        {
                            ISolutionItem solutionItem = obj as ISolutionItem;
                            Children.Remove(itemToViewmodel[solutionItem]);
                            itemToViewmodel.Remove(solutionItem);
                        }
                    }
                };
            }
        }

        public ObservableCollection<SolutionItemViewModel> Children { get; }

        public string Name => itemNameRegistry.GetName(Item);
        public string ExtraId => Item.ExtraId;
        public bool IsContainer => Item.IsContainer;
        public bool IsExportable => Item.IsExportable;
        public ISolutionItem Item { get; }

        public SolutionItemViewModel Parent { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => SetProperty(ref isExpanded, value);
        }

        private void AddItem(ISolutionItem item, int index = -1)
        {
            if (!itemToViewmodel.TryGetValue(item, out SolutionItemViewModel viewModel))
            {
                viewModel = new SolutionItemViewModel(itemNameRegistry, item, this);
                itemToViewmodel[item] = viewModel;
            }
            else
                viewModel.Parent = this;

            Children.Insert(index < 0 ? Children.Count : index, viewModel);
        }

        public void AddViewModel(SolutionItemViewModel sourceItem)
        {
            itemToViewmodel[sourceItem.Item] = sourceItem;
        }

        public void Refresh()
        {
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.Refresh();
                }
            }
            RaisePropertyChanged(nameof(ExtraId));
            RaisePropertyChanged(nameof(Name));
        }
    }
}