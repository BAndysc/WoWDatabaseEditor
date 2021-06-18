using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.Solutions.Explorer.ViewModels
{
    [AutoRegister]
    [SingleInstance]
    public class SolutionExplorerViewModel : BindableBase, ITool, IDropTarget
    {
        private readonly IEventAggregator ea;
        private readonly ISolutionItemNameRegistry itemNameRegistry;

        private readonly Dictionary<ISolutionItem, SolutionItemViewModel> itemToViewmodel;
        private readonly ISolutionManager solutionManager;
        private readonly IStatusBar statusBar;
        private readonly ISolutionItemIconRegistry solutionItemIconRegistry;

        private SolutionItemViewModel? selected;
        private bool visibility;

        public string UniqueId => "solution_explorer";
        public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Right;
        public bool OpenOnStart => true;

        public SolutionExplorerViewModel(ISolutionItemNameRegistry itemNameRegistry,
            ISolutionManager solutionManager,
            IEventAggregator ea,
            ISolutionSqlService solutionSqlService,
            INewItemService newItemService,
            ISolutionTasksService solutionTasksService,
            IStatusBar statusBar,
            ISolutionItemIconRegistry solutionItemIconRegistry,
            ISolutionItemProvideService provider)
        {
            this.itemNameRegistry = itemNameRegistry;
            this.solutionManager = solutionManager;
            this.ea = ea;
            this.statusBar = statusBar;
            this.solutionItemIconRegistry = solutionItemIconRegistry;

            Root = new ObservableCollection<SolutionItemViewModel>();
            itemToViewmodel = new Dictionary<ISolutionItem, SolutionItemViewModel>();

            foreach (ISolutionItem item in this.solutionManager.Items)
                AddItemToRoot(item);

            this.solutionManager.Items.CollectionChanged += (sender, args) =>
            {
                if (args.NewItems != null)
                {
                    var i = 0;
                    foreach (ISolutionItem obj in args.NewItems)
                    {
                        AddItemToRoot(obj, args.NewStartingIndex + i);
                        i++;
                    }
                }

                if (args.OldItems != null)
                {
                    foreach (ISolutionItem obj in args.OldItems)
                    {
                        ISolutionItem solutionItem = obj;
                        Root.Remove(itemToViewmodel[solutionItem]);
                        itemToViewmodel.Remove(solutionItem);
                    }
                }
            };

            solutionManager.RefreshRequest += item =>
            {
                foreach (var root in Root)
                {
                    root.Refresh();
                }
            };

            Dictionary<string, AddItemCategoryMenuViewModel> byNameCategories = new();
            Func<ISolutionItemProvider, Task> insertItemCommand = async provider =>
            {
                var item = await provider.CreateSolutionItem();
                if (item != null)
                    DoAddItem(item);
            };
            foreach (var item in provider.AllCompatible)
            {
                if (item is INamedSolutionItemProvider)
                    continue;
                
                if (!byNameCategories.TryGetValue(item.GetGroupName(), out var category))
                {
                    category = new AddItemCategoryMenuViewModel(item.GetGroupName());
                    byNameCategories.Add(category.Name, category);
                    AddItems.Add(category);
                }
                
                category.Items.Add(new SolutionItemMenuViewModel(item, insertItemCommand));
            }

            AddItem = new DelegateCommand(async () =>
            {
                ISolutionItem? item = await newItemService.GetNewSolutionItem();
                if (item != null)
                    DoAddItem(item);
            }, () => (SelectedItem == null || SelectedItem.IsContainer) && SelectedItems.Count <= 1)
                .ObservesProperty(() => SelectedItem)
                .ObservesProperty(() => SelectedItems.Count);

            RemoveItem = new DelegateCommand(() =>
            {
                if (SelectedItems.Count > 0)
                {
                    foreach (var item in SelectedItems.ToList())
                    {
                        DeleteSolutionItem(item);
                    }
                    SelectedItems.Clear();
                }
                else if (selected != null)
                {
                    DeleteSolutionItem(selected);
                }
            }, () => SelectedItem != null || SelectedItems.Count > 0)
                .ObservesProperty(() => SelectedItem)
                .ObservesProperty(() => SelectedItems.Count);

            SelectedItemChangedCommand = new DelegateCommand<SolutionItemViewModel>(ob => { selected = ob; });

            RequestOpenItem = new DelegateCommand<SolutionItemViewModel>(item =>
            {
                if (item != null && !item.IsContainer)
                    this.ea.GetEvent<EventRequestOpenItem>().Publish(item.Item);
            });

            GenerateSQL = new DelegateCommand(() =>
            {
                if (selected != null)
                {
                    solutionSqlService.OpenDocumentWithSqlFor(selected.Item);
                }
            });

            UpdateDatabase = new DelegateCommand(() =>
            {
                if (selected != null)
                    solutionTasksService.SaveSolutionToDatabaseTask(selected.Item);
            }, () => solutionTasksService.CanSaveToDatabase);
            
            ExportToServer = new DelegateCommand(() =>
            {
                if (selected != null)
                    solutionTasksService.SaveAndReloadSolutionTask(selected.Item);
            }, () => solutionTasksService.CanSaveAndReloadRemotely);

            ExportToServerItem = new DelegateCommand<SolutionItemViewModel>(item =>
            {
                if (item != null)
                    solutionTasksService.SaveAndReloadSolutionTask(item.Item);
            }, item => solutionTasksService.CanSaveAndReloadRemotely);
        }

        private void DeleteSolutionItem(SolutionItemViewModel item)
        {
            if (item.Parent == null)
                this.solutionManager.Items.Remove(item.Item);
            else
                item.Parent.Item.Items?.Remove(item.Item);
            
            if (item == SelectedItem)
                SelectedItem = null;
        }

        private void DoAddItem(ISolutionItem item)
        {
            if (selected == null)
                solutionManager.Items.Add(item);
            else if (selected.Item.Items != null && selected.Item.IsContainer)
                selected.Item.Items.Add(item);
            else if (selected.Parent != null)
            {
                var indexOf = selected.Parent.Item.Items?.IndexOf(selected.Item) ?? -1;
                if (indexOf != -1)
                    selected.Parent.Item.Items!.Insert(indexOf + 1, item);
            }
                    
            if (item is not SolutionFolderItem)
                ea.GetEvent<EventRequestOpenItem>().Publish(item);
        }

        public ObservableCollection<SolutionItemViewModel> Root { get; }
        public ObservableCollection<AddItemCategoryMenuViewModel> AddItems { get; } = new();
        public DelegateCommand AddItem { get; }
        public DelegateCommand RemoveItem { get; }
        public DelegateCommand GenerateSQL { get; }
        public DelegateCommand<SolutionItemViewModel> SelectedItemChangedCommand { get; }
        public DelegateCommand<SolutionItemViewModel> RequestOpenItem { get; }
        public DelegateCommand ExportToServer { get; }
        public DelegateCommand<SolutionItemViewModel> ExportToServerItem { get; }
        public DelegateCommand UpdateDatabase { get; }
        
        public void DragOver(IDropInfo dropInfo)
        {
            SolutionItemViewModel? sourceItem = dropInfo.Data as SolutionItemViewModel;
            SolutionItemViewModel? targetItem = dropInfo.TargetItem as SolutionItemViewModel;

            if (sourceItem != null)
            {
                bool highlight = dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter) &&
                                 (targetItem?.IsContainer ?? false);

                dropInfo.DropTargetAdorner = highlight ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            SolutionItemViewModel? sourceItem = dropInfo.Data as SolutionItemViewModel;
            SolutionItemViewModel? targetItem = dropInfo.TargetItem as SolutionItemViewModel;

            if (sourceItem == null)
                return;

            var prevPosition = 0;
            var sourceList = sourceItem.Parent == null ? solutionManager.Items : sourceItem.Parent.Item.Items;
            if (sourceList == null)
                return;
            
            SolutionItemViewModel? destListOwner = dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight
                ? targetItem
                : targetItem?.Parent;
            
            var destList = destListOwner?.Item?.Items ?? solutionManager.Items;

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
                Debug.Assert(targetItem != null && targetItem.Item.Items != null);
                targetItem.AddViewModel(sourceItem);
                targetItem.Item.Items.Add(sourceItem.Item);
            }
            else
            {
                if (targetItem == null || targetItem.Parent == null)
                    itemToViewmodel[sourceItem.Item] = sourceItem;
                else
                    targetItem.Parent.AddViewModel(sourceItem);

                int destPosition = dropInfo.InsertIndex + (dropInfo.InsertPosition == RelativeInsertPosition.AfterTargetItem ? 1 : 0);
                if (destList == sourceList && dropInfo.InsertIndex >= prevPosition)
                    destPosition--;

                destList.Insert(Math.Clamp(destPosition, 0, destList.Count), sourceItem.Item);
            }
        }
        
        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        
        public string Title { get; } = "Solution explorer";

        public bool Visibility
        {
            get => visibility;
            set => SetProperty(ref visibility, value);
        }

        public SolutionItemViewModel? SelectedItem
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        private ObservableCollection<SolutionItemViewModel> selectedItems = new();
        public ObservableCollection<SolutionItemViewModel> SelectedItems
        {
            get => selectedItems;
            set => SetProperty(ref selectedItems, value);
        }
        
        private void AddItemToRoot(ISolutionItem item, int index = -1)
        {
            if (!itemToViewmodel.TryGetValue(item, out SolutionItemViewModel? viewModel))
            {
                viewModel = new SolutionItemViewModel(solutionItemIconRegistry, itemNameRegistry, item);
                itemToViewmodel[item] = viewModel;
            }
            else
                viewModel.Parent = null;

            Root.Insert(index < 0 ? Root.Count : index, viewModel);
        }
    }
}
