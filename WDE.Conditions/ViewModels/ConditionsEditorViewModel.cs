using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using WDE.Conditions.Data;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.Parameters.Models;

namespace WDE.Conditions.ViewModels
{
    [AutoRegister]
    internal class ConditionsEditorViewModel : ObservableBase, IDialog, IDropTarget
    {
        private const int ConditionOr = -1;
        private readonly IConditionsFactory conditionsFactory;
        private readonly IConditionDataManager conditionDataManager;
        private ConditionsEditorHistoryHandler historyHandler;

        public ConditionsEditorViewModel(
            IConditionsFactory conditionsFactory,
            IConditionDataManager conditionDataManager,
            IItemFromListProvider itemFromListProvider,
            IParameterPickerService parameterPickerService,
            IHistoryManager historyManager,
            IEnumerable<ICondition>? conditions,
            int conditionSourceType)
        {
            this.conditionsFactory = conditionsFactory;
            this.conditionDataManager = conditionDataManager;
            this.HistoryManager = historyManager;

            ConditionTypes = conditionDataManager
                .GetConditionGroups()
                .SelectMany(group => group.Members)
                .Where(conditionDataManager.HasConditionData)
                .Select(conditionDataManager.GetConditionData)
                .ToList();
            
            if (conditions != null)
            {
                int previousElseGroup = -1;
                foreach (var c in conditions)
                {
                    var vm = conditionsFactory.Create(conditionSourceType, c);
                    if (vm == null)
                        continue;

                    if (c.ElseGroup != previousElseGroup && previousElseGroup != -1)
                        Conditions.Add(conditionsFactory.CreateOr(conditionSourceType));

                    previousElseGroup = c.ElseGroup;
                    Conditions.Add(vm);
                }
            }

            Accept = new DelegateCommand(() => CloseOk?.Invoke());
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
            PickCommand = new AsyncAutoCommand<IParameterValueHolder>(async prh =>
            {
                if (prh is ParameterValueHolder<long> longParam)
                {
                    if (!longParam.HasItems) 
                        return;

                    var (newItem, ok) = await parameterPickerService.PickParameter(longParam.Parameter, longParam.Value);
                    if (ok)
                        longParam.Value = newItem;   
                }
                
                if (prh is ParameterValueHolder<string> stringParam)
                {
                    if (!stringParam.HasItems) 
                        return;

                    var (newItem, ok) = await parameterPickerService.PickParameter(stringParam.Parameter, stringParam.Value);
                    if (ok)
                        stringParam.Value = newItem ?? "";   
                }
            });
            AddItemCommand = new DelegateCommand(() =>
            {
                int index = Conditions.Count;
                if (SelectedCondition != null)
                    index = Conditions.IndexOf(SelectedCondition) + 1;
                index = Math.Clamp(index, 0, Conditions.Count);

                var item = conditionsFactory.Create(conditionSourceType, 0) ??
                           conditionsFactory.CreateOr(conditionSourceType);
                
                if (item == null)
                    throw new Exception();
                Conditions.Insert(index, item);
            });
            RemoveItemCommand = new DelegateCommand(() =>
            {
                if (SelectedCondition == null)
                    return;
                
                int indexOf = Conditions.IndexOf(SelectedCondition);
                if (indexOf != -1)
                {
                    Conditions.RemoveAt(indexOf);
                    if (indexOf - 1 >= 0 && Conditions.Count > 0)
                        SelectedCondition = Conditions[indexOf - 1];
                    else if (Conditions.Count > 0)
                        SelectedCondition = Conditions[indexOf];
                }
                    
            }, () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);
            CopyCommand = new DelegateCommand(() =>
            {
                if (SelectedCondition != null)
                    Clipboard = SelectedCondition?.ToCondition(0);
            }, () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);
            CutCommand = new DelegateCommand(() =>
            {
                if (SelectedCondition != null)
                {
                    Clipboard = SelectedCondition?.ToCondition(0);
                    Conditions.Remove(SelectedCondition!);
                    SelectedCondition = null;
                }
            }, () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);
            PasteCommand = new DelegateCommand(() =>
            {
                if (clipboard != null)
                {
                    int indexOf = Conditions.Count;
                    if (SelectedCondition != null)
                        indexOf = Conditions.IndexOf(SelectedCondition) + 1;
                    var item = conditionsFactory.Create(conditionSourceType, clipboard);
                    if (item != null)
                        Conditions.Insert(indexOf, item);   
                }
            }, () => Clipboard != null).ObservesProperty(() => Clipboard);

            UndoCommand =
                new DelegateCommand(HistoryManager.Undo, () => HistoryManager.CanUndo).ObservesProperty(() =>
                    HistoryManager.CanUndo);
            RedoCommand =
                new DelegateCommand(HistoryManager.Redo, () => HistoryManager.CanRedo).ObservesProperty(() =>
                    HistoryManager.CanRedo);

            Watch(this, t => t.SelectedCondition, nameof(SelectedConditionsType));

            historyHandler = AutoDispose(new ConditionsEditorHistoryHandler(this, conditionsFactory));
            HistoryManager.AddHandler(historyHandler);
        }

        private ICondition? clipboard;
        private ICondition? Clipboard
        {
            get => clipboard;
            set => SetProperty(ref clipboard, value);
        }
        
        public ICommand PickCommand { get; }
        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public ICommand AddItemCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand CutCommand { get; }
        public DelegateCommand RemoveItemCommand { get; }

        public IHistoryManager HistoryManager { get; }
        
        public ConditionJsonData? SelectedConditionsType
        {
            get => SelectedCondition?.ConditionId == null
                ? null
                : conditionDataManager.GetConditionData(SelectedCondition!.ConditionId);
            set
            {
                if (SelectedCondition == null || value == null)
                    return;
                
                conditionsFactory.Update(value.Id, SelectedCondition);
            }
        }

        public ObservableCollection<ConditionViewModel> Conditions { get; } = new();
        public IList<ConditionJsonData> ConditionTypes { get; }
        
        private ConditionViewModel? selected;
        public ConditionViewModel? SelectedCondition
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }
        
        public int DesiredWidth => 800;
        public int DesiredHeight => 600;
        public string Title { get; set; } = "Conditions edit";
        public bool Resizeable => true;

        public event Action? CloseCancel;
        public event Action? CloseOk;

        public IEnumerable<ICondition> GenerateConditions()
        {
            int elseGroup = 0;
            bool previousWasOr = true;
            foreach (var c in Conditions)
            {
                if (c.ConditionId == ConditionOr)
                {
                    if (!previousWasOr)
                    {
                        elseGroup++;
                        previousWasOr = true;
                    }
                }
                else
                {
                    previousWasOr = false;
                    yield return c.ToCondition(elseGroup);
                }
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not ConditionViewModel data)
                return;

            int indexOf = Conditions.IndexOf(data);
            int dropIndex = dropInfo.InsertIndex;
            if (indexOf < dropIndex)
                dropIndex--;

            using var bulk = historyHandler.BulkEdit("Reorder conditions");
            Conditions.RemoveAt(indexOf);
            Conditions.Insert(dropIndex, data);
            SelectedCondition = data;
        }
    }
}