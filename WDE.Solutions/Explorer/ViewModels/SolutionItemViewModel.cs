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
        private readonly SolutionItemViewModel _parent;

        private readonly ObservableCollection<SolutionItemViewModel> _children;
        public ObservableCollection<SolutionItemViewModel> Children => _children;

        private Dictionary<ISolutionItem, SolutionItemViewModel> _itemToViewmodel;

        public string Name => itemNameRegistry.GetName(_item);
        public string ExtraId => _item.ExtraId;
        public bool IsContainer => _item.IsContainer;
        public bool IsExportable => _item.IsExportable;
        public ISolutionItem Item => _item;
        public SolutionItemViewModel Parent => _parent;

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
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
                        foreach (object obj in args.NewItems)
                            AddItem(obj as ISolutionItem);

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

        private void AddItem(ISolutionItem item)
        {
            var viewModel = new SolutionItemViewModel(itemNameRegistry, item, this);
            _children.Add(viewModel);
            _itemToViewmodel.Add(item, viewModel);
        }
    }
}
