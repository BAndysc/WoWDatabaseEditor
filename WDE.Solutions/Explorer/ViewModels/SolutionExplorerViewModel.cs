using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GongSolutions.Wpf.DragDrop;
using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Solution;
using Prism.Ioc;
using WDE.Common.Managers;

namespace WDE.Solutions.Explorer.ViewModels
{
    public class SolutionExplorerViewModel : BindableBase, ITool, IDropTarget
    {
        private readonly ISolutionItemNameRegistry itemNameRegistry;
        private readonly ISolutionManager _solutionManager;
        private readonly IEventAggregator _ea;
        private readonly IStatusBar _statusBar;

        private ObservableCollection<SolutionItemViewModel> _firstGeneration;
        public ObservableCollection<SolutionItemViewModel> Root => _firstGeneration;

        public DelegateCommand AddItem { get; private set; }
        public DelegateCommand RemoveItem { get; private set; }
        public DelegateCommand GenerateSQL { get; private set; }
        public DelegateCommand<SolutionItemViewModel> SelectedItemChangedCommand { get; private set; }
        public DelegateCommand<SolutionItemViewModel> RequestOpenItem { get; private set; }

        private SolutionItemViewModel _selected;
        private Dictionary<ISolutionItem, SolutionItemViewModel> _itemToViewmodel;

        public SolutionExplorerViewModel(ISolutionItemNameRegistry itemNameRegistry, 
            ISolutionManager solutionManager,
            IEventAggregator ea, 
            INewItemService newItemService, 
            IStatusBar statusBar,
            ISolutionItemSqlGeneratorRegistry sqlGeneratorRegistry)
        {
            this.itemNameRegistry = itemNameRegistry;
            _solutionManager = solutionManager;
            _ea = ea;
            _statusBar = statusBar;

            _firstGeneration = new ObservableCollection<SolutionItemViewModel>();
            _itemToViewmodel = new Dictionary<ISolutionItem, SolutionItemViewModel>();

            foreach (var item in _solutionManager.Items)
            {
                AddItemToRoot(item);
            }

            _solutionManager.Items.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems != null)
                {
                    int i = 0;
                    foreach (var obj in args.NewItems)
                    {
                        AddItemToRoot(obj as ISolutionItem, args.NewStartingIndex + i);
                        i++;
                    }
                }

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
                if (item != null && !item.IsContainer)
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

        private void AddItemToRoot(ISolutionItem item, int index = -1)
        {
            if (!_itemToViewmodel.TryGetValue(item, out var viewModel))
            {
                viewModel = new SolutionItemViewModel(itemNameRegistry, item);
                _itemToViewmodel[item] = viewModel;
            }
            else
                viewModel.Parent = null;    
            Root.Insert(index < 0 ? Root.Count : index, viewModel);
        }

        public string Title { get; } = "Solution explorer";
        private Visibility _visibility;
        public Visibility Visibility
        {
            get => _visibility;
            set => SetProperty(ref _visibility, value);
        }

        public void DragOver(IDropInfo dropInfo)
        {
            SolutionItemViewModel sourceItem = dropInfo.Data as SolutionItemViewModel;
            SolutionItemViewModel targetItem = dropInfo.TargetItem as SolutionItemViewModel;
		
            if (sourceItem != null)
            {
                var highlight = dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter) &&
                                (targetItem?.IsContainer ?? false);
            
                dropInfo.DropTargetAdorner = highlight ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            SolutionItemViewModel sourceItem = dropInfo.Data as SolutionItemViewModel;
            SolutionItemViewModel targetItem = dropInfo.TargetItem as SolutionItemViewModel;
            
            if (sourceItem == null)
                return;
            
            int prevPosition = 0;
            var sourceList = sourceItem.Parent == null ? _solutionManager.Items : sourceItem.Parent.Item.Items;
            var destListOwner = (dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                ? targetItem
                : targetItem?.Parent;
            var destList = destListOwner?.Item?.Items ?? _solutionManager.Items;

            while (destListOwner != null)
            {
                if (sourceItem.Item == destListOwner.Item)
                    return;
                destListOwner = destListOwner.Parent;
            }
            
            prevPosition = sourceList.IndexOf(sourceItem.Item);
            if (prevPosition >= 0)
                sourceList.RemoveAt(prevPosition);

            if (dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
            {
                targetItem.AddViewModel(sourceItem);
                targetItem.Item.Items.Add(sourceItem.Item);
            }
            else
            {
                if (targetItem == null || targetItem.Parent == null)
                    _itemToViewmodel[sourceItem.Item] = sourceItem;
                else
                    targetItem.Parent.AddViewModel(sourceItem);
                
                var destPosition = dropInfo.InsertIndex;
                if (destList == sourceList && dropInfo.InsertIndex >= prevPosition)
                    destPosition--;
                    
                destList.Insert(destPosition, sourceItem.Item);
            }
        }
    }
}
