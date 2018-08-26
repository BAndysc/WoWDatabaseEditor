using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Solution;
using Prism.Ioc;

namespace WDE.Solutions.Explorer.ViewModels
{
    public class SolutionExplorerViewModel : BindableBase
    {
        private readonly ISolutionItemNameRegistry itemNameRegistry;
        private readonly ISolutionManager _solutionManager;
        private readonly IEventAggregator _ea;

        private ObservableCollection<SolutionItemViewModel> _firstGeneration;
        public ObservableCollection<SolutionItemViewModel> Root => _firstGeneration;

        public DelegateCommand AddItem { get; private set; }
        public DelegateCommand RemoveItem { get; private set; }
        public DelegateCommand GenerateSQL { get; private set; }
        public DelegateCommand<SolutionItemViewModel> SelectedItemChangedCommand { get; private set; }
        public DelegateCommand<SolutionItemViewModel> RequestOpenItem { get; private set; }

        private SolutionItemViewModel _selected;
        private Dictionary<ISolutionItem, SolutionItemViewModel> _itemToViewmodel;

        public SolutionExplorerViewModel(ISolutionItemNameRegistry itemNameRegistry, ISolutionManager solutionManager, IEventAggregator ea, INewItemService newItemService, ISolutionItemSqlGeneratorRegistry sqlGeneratorRegistry)
        {
            this.itemNameRegistry = itemNameRegistry;
            _solutionManager = solutionManager;
            _ea = ea;

            _firstGeneration = new ObservableCollection<SolutionItemViewModel>();
            _itemToViewmodel = new Dictionary<ISolutionItem, SolutionItemViewModel>();

            foreach (var item in _solutionManager.Items)
            {
                AddItemToRoot(item);
            }

            _solutionManager.Items.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems != null)
                    foreach (var obj in args.NewItems)
                        AddItemToRoot(obj as ISolutionItem);

                if (args.OldItems != null)
                    foreach (var obj in args.OldItems)
                    {
                        var solutionItem = obj as ISolutionItem;
                        Root.Remove(_itemToViewmodel[solutionItem]);
                        _itemToViewmodel.Remove(solutionItem);
                    }
            };

            AddItem = new DelegateCommand(() =>
            {
                ISolutionItem item = newItemService.GetNewSolutionItem();
                if (item != null)
                {
                    if (_selected == null)
                        solutionManager.Items.Add(item);    
                    else
                        _selected.Item.Items.Add(item);
                }
            });

            RemoveItem = new DelegateCommand(() =>
            {
                if (_selected != null)
                {
                    if (_selected.Parent == null)
                        _solutionManager.Items.Remove(_selected.Item);
                    else
                    {
                        _selected.Parent.Item.Items.Remove(_selected.Item);
                    }
                }
            });

            SelectedItemChangedCommand = new DelegateCommand<SolutionItemViewModel>((ob) =>
            {
                _selected = ob;
            });

            RequestOpenItem = new DelegateCommand<SolutionItemViewModel>((item) =>
            {
                if (!item.IsContainer)
                    _ea.GetEvent<EventRequestOpenItem>().Publish(item.Item);
            });

            GenerateSQL = new DelegateCommand(() =>
            {
                if (_selected != null)
                {
                    MetaSolutionSQL solution = new MetaSolutionSQL(sqlGeneratorRegistry.GenerateSql(_selected.Item));
                    _ea.GetEvent<EventRequestOpenItem>().Publish(solution);
                }
            });
        }

        private void AddItemToRoot(ISolutionItem item)
        {
            var viewModel = new SolutionItemViewModel(itemNameRegistry, item);
            _itemToViewmodel.Add(item, viewModel);
            Root.Add(viewModel);
        }
    }
}
