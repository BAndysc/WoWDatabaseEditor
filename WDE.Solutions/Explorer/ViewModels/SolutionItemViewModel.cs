using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WDE.Common;

namespace WDE.Solutions.Explorer.ViewModels
{
    public class SolutionItemViewModel : BindableBase
    {
        private readonly ISolutionItem _item;
        private readonly SolutionItemViewModel _parent;

        private readonly ObservableCollection<SolutionItemViewModel> _children;
        public ObservableCollection<SolutionItemViewModel> Children => _children;

        public string Name => _item.Name;
        public string ExtraId => _item.ExtraId;
        public bool IsContainer => _item.IsContainer;
        public bool IsExportable => _item.IsExportable;
        public ISolutionItem Item => _item;

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        public SolutionItemViewModel(ISolutionItem item) : this(item, null)
        {
        }

        public SolutionItemViewModel(ISolutionItem item, SolutionItemViewModel parent)
        {
            _item = item;
            _parent = parent;

            if (item.Items != null)
            {
                _children = new ObservableCollection<SolutionItemViewModel>(
               (from child in item.Items
                select new SolutionItemViewModel(child, this))
                .ToList());

                item.Items.CollectionChanged += (sender, args) =>
                {
                    foreach (object t in args.NewItems)
                        _children.Add(new SolutionItemViewModel(t as ISolutionItem, this));
                };
            }

        }
    }
}
