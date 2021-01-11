using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WDE.Common;
using WDE.Common.Solution;

namespace WDE.Solutions.Explorer.ViewModels
{
    public class SolutionItemViewModel : BindableBase
    {
        private ISolutionItemNameRegistry itemNameRegistry;

        private readonly ISolutionItem _item;
        private SolutionItemViewModel _parent;

        private readonly ObservableCollection<SolutionItemViewModel> _children;
        public ObservableCollection<SolutionItemViewModel> Children => _children;

        private Dictionary<ISolutionItem, SolutionItemViewModel> _itemToViewmodel;

        public string Name => itemNameRegistry.GetName(_item);
        public string ExtraId => _item.ExtraId;
        public bool IsContainer => _item.IsContainer;
        public bool IsExportable => _item.IsExportable;
        public ISolutionItem Item => _item;
        public SolutionItemViewModel Parent
        {
            get => _parent;
            set => _parent = value;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public SolutionItemViewModel(ISolutionItemNameRegistry itemNameRegistry, ISolutionItem item) : this(itemNameRegistry, item, null)
        {
        }

        public SolutionItemViewModel(ISolutionItemNameRegistry itemNameRegistry, ISolutionItem item, SolutionItemViewModel parent)
        {
            this.itemNameRegistry = itemNameRegistry;
            _item = item;
            _parent = parent;

            _itemToViewmodel = new Dictionary<ISolutionItem, SolutionItemViewModel>();

            if (item.Items != null)
            {
                _children = new ObservableCollection<SolutionItemViewModel>();

                foreach (object obj in item.Items)
                    AddItem(obj as ISolutionItem);

                item.Items.CollectionChanged += (sender, args) =>
                {
                    if (args.NewItems != null)
                    {
                        int i = 0;
                        foreach (object obj in args.NewItems)
                            AddItem(obj as ISolutionItem, args.NewStartingIndex + i);
                    }

                    if (args.OldItems != null)
                        foreach (object obj in args.OldItems)
                        {
                            var solutionItem = obj as ISolutionItem;
                            _children.Remove(_itemToViewmodel[solutionItem]);
                            _itemToViewmodel.Remove(solutionItem);
                        }
                };
            }
        }

        private void AddItem(ISolutionItem item, int index = -1)
        {
            if (!_itemToViewmodel.TryGetValue(item, out var viewModel))
            {
                viewModel = new SolutionItemViewModel(itemNameRegistry, item, this);
                _itemToViewmodel[item] = viewModel;
            }
            else
                viewModel.Parent = this;
            _children.Insert(index < 0 ? _children.Count : index, viewModel);
        }

        public void AddViewModel(SolutionItemViewModel sourceItem)
        {
            _itemToViewmodel[sourceItem.Item] = sourceItem;
        }
    }
}
