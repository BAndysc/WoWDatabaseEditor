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
        private readonly ISolutionManager _solutionManager;
        private readonly IEventAggregator _ea;

        private ObservableCollection<SolutionItemViewModel> _firstGeneration;
        public ObservableCollection<SolutionItemViewModel> Root => _firstGeneration;

        public DelegateCommand AddItem { get; private set; }
        public DelegateCommand GenerateSQL { get; set; }
        public DelegateCommand<SolutionItemViewModel> SelectedItemChangedCommand { get; private set; }
        public DelegateCommand<SolutionItemViewModel> RequestOpenItem { get; private set; }

        private SolutionItemViewModel _selected;
        
        public SolutionExplorerViewModel(ISolutionManager solutionManager, IEventAggregator ea, INewItemService newItemService)
        {
            _solutionManager = solutionManager;
            _ea = ea;

            _firstGeneration = new ObservableCollection<SolutionItemViewModel>();

            foreach (var item in _solutionManager.Items)
            {
                Root.Add(new SolutionItemViewModel(item));
            }

            _solutionManager.Items.CollectionChanged += (sender, args) =>
            {
                foreach (var obj in args.NewItems)
                {
                    Root.Add(new SolutionItemViewModel(obj as ISolutionItem));
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
                    MetaSolutionSQL solution = new MetaSolutionSQL(_selected.Item.ExportSql);
                    _ea.GetEvent<EventRequestOpenItem>().Publish(solution);
                }
            });
        }
    }
}
