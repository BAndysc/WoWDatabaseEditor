using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using WDE.CMangosConditions.Data;
using WDE.CMangosConditions.Models;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Common.Utils.DragDrop;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.Parameters.Models;

namespace WDE.CMangosConditions.ViewModels
{
    [AutoRegister]
    internal class MangosConditionsEditorViewModel : ObservableBase, IDialog, IDropTarget
    {
        private readonly IMangosConditionsFactory conditionsFactory;
        private readonly IMangosConditionDataManager dataManager;
        private readonly bool requireSingleRoot;
        private readonly MangosConditionsHistoryHandler historyHandler;

        public MangosConditionsEditorViewModel(
            IMangosConditionsFactory conditionsFactory,
            IMangosConditionDataManager dataManager,
            IParameterPickerService parameterPickerService,
            IHistoryManager historyManager,
            IReadOnlyList<IMangosConditionLine> conditions,
            bool requireSingleRoot)
        {
            this.conditionsFactory = conditionsFactory;
            this.dataManager = dataManager;
            this.requireSingleRoot = requireSingleRoot;
            HistoryManager = historyManager;

            ConditionTypes = dataManager.AllConditions.ToList();

            foreach (var root in MangosConditionTreeCodec.BuildTree(conditions, conditionsFactory))
                Roots.Add(root);

            Accept = new DelegateCommand(() =>
            {
                var errors = MangosConditionTreeCodec.Validate(Roots);
                if (requireSingleRoot && Roots.Count != 1)
                    errors.Insert(0, Roots.Count == 0
                        ? "Add a condition first."
                        : "Exactly one root condition is required here (wrap the roots in an AND/OR).");
                Errors = string.Join("\n", errors);
                if (Errors.Length == 0)
                    CloseOk?.Invoke();
            });
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
            });

            AddRootCommand = new DelegateCommand(() =>
            {
                var item = conditionsFactory.Create(0);
                ApplySourceTarget(item);
                Roots.Add(item);
                SelectedCondition = item;
            });

            AddChildCommand = new DelegateCommand(() =>
            {
                if (SelectedCondition is not { } parent || !parent.IsLogical || parent.Children.Count >= parent.MaxChildren)
                    return;
                var item = conditionsFactory.Create(0);
                ApplySourceTarget(item);
                item.Parent = parent;
                parent.Children.Add(item);
                parent.IsExpanded = true;
                SelectedCondition = item;
            }, () => SelectedCondition is { IsLogical: true } s && s.Children.Count < s.MaxChildren)
                .ObservesProperty(() => SelectedCondition);

            RemoveItemCommand = new DelegateCommand(() =>
            {
                if (SelectedCondition == null)
                    return;
                var node = SelectedCondition;
                var owner = OwnerCollection(node);
                int index = owner.IndexOf(node);
                if (index == -1)
                    return;
                owner.RemoveAt(index);
                SelectedCondition = owner.Count > 0
                    ? owner[Math.Min(index, owner.Count - 1)]
                    : node.Parent;
            }, () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);

            WrapInAndCommand = new DelegateCommand(() => WrapSelected(MangosConditionTreeCodec.TypeAnd), () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);
            WrapInOrCommand = new DelegateCommand(() => WrapSelected(MangosConditionTreeCodec.TypeOr), () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);
            WrapInNotCommand = new DelegateCommand(() => WrapSelected(MangosConditionTreeCodec.TypeNot), () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);

            CopyCommand = new DelegateCommand(() =>
            {
                if (SelectedCondition != null)
                    Clipboard = CloneNode(SelectedCondition);
            }, () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);

            CutCommand = new DelegateCommand(() =>
            {
                if (SelectedCondition == null)
                    return;
                Clipboard = CloneNode(SelectedCondition);
                RemoveItemCommand.Execute();
            }, () => SelectedCondition != null).ObservesProperty(() => SelectedCondition);

            PasteCommand = new DelegateCommand(() =>
            {
                if (clipboard == null)
                    return;
                var item = CloneNode(clipboard);
                if (SelectedCondition is { IsLogical: true } parent && parent.Children.Count < parent.MaxChildren)
                {
                    item.Parent = parent;
                    parent.Children.Add(item);
                    parent.IsExpanded = true;
                }
                else
                {
                    item.Parent = null;
                    Roots.Add(item);
                }
                SelectedCondition = item;
            }, () => Clipboard != null).ObservesProperty(() => Clipboard);

            UndoCommand = new DelegateCommand(historyManager.Undo, () => historyManager.CanUndo)
                .ObservesProperty(() => HistoryManager.CanUndo);
            RedoCommand = new DelegateCommand(historyManager.Redo, () => historyManager.CanRedo)
                .ObservesProperty(() => HistoryManager.CanRedo);

            Watch(this, t => t.SelectedCondition, nameof(SelectedConditionsType));

            historyHandler = AutoDispose(new MangosConditionsHistoryHandler(this, conditionsFactory));
            HistoryManager.AddHandler(historyHandler);
        }

        public ObservableCollection<MangosConditionViewModel> Roots { get; } = new();
        public IList<MangosConditionJson> ConditionTypes { get; }
        public IHistoryManager HistoryManager { get; }

        private MangosConditionSourceTarget? sourceTarget;

        /// <summary>What the caller will pass as the conditions' source/target objects.</summary>
        public void SetSourceTarget(MangosConditionSourceTarget? context)
        {
            sourceTarget = context;
            foreach (var root in Roots)
                ApplySourceTarget(root);
            RaisePropertyChanged(nameof(SourceTargetHint));
            RaisePropertyChanged(nameof(HasSourceTargetHint));
        }

        private void ApplySourceTarget(MangosConditionViewModel node)
        {
            foreach (var descendant in node.Descendants())
                descendant.SetSourceTarget(sourceTarget);
        }

        public bool HasSourceTargetHint => sourceTarget != null;
        public string SourceTargetHint => sourceTarget == null
            ? ""
            : $"Target: {sourceTarget.Target}    Source: {sourceTarget.Source ?? "(none — the core passes no source object here)"}";

        private MangosConditionViewModel? selected;
        public MangosConditionViewModel? SelectedCondition
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }

        public MangosConditionJson? SelectedConditionsType
        {
            get => SelectedCondition == null ? null : dataManager.TryGetCondition(SelectedCondition.ConditionType);
            set
            {
                if (SelectedCondition == null || value == null || value.Id == SelectedCondition.ConditionType)
                    return;
                ChangeType(SelectedCondition, value);
            }
        }

        private void ChangeType(MangosConditionViewModel node, MangosConditionJson newType)
        {
            using var bulk = historyHandler.BulkEdit("Change condition type");
            if (newType.MaxChildren == 0 && node.Children.Count > 0)
            {
                // leaf types cannot hold children: move them up to the node's level
                var owner = OwnerCollection(node);
                int at = owner.IndexOf(node) + 1;
                while (node.Children.Count > 0)
                {
                    var child = node.Children[0];
                    node.Children.RemoveAt(0);
                    child.Parent = node.Parent;
                    owner.Insert(at++, child);
                }
            }
            conditionsFactory.Update(newType.Id, node);
            if (newType.MaxChildren > 0 && node.Children.Count > newType.MaxChildren)
            {
                var owner = OwnerCollection(node);
                int at = owner.IndexOf(node) + 1;
                while (node.Children.Count > newType.MaxChildren)
                {
                    var child = node.Children[^1];
                    node.Children.RemoveAt(node.Children.Count - 1);
                    child.Parent = node.Parent;
                    owner.Insert(at, child);
                }
            }
        }

        private void WrapSelected(int logicalType)
        {
            if (SelectedCondition == null)
                return;
            var node = SelectedCondition;
            var owner = OwnerCollection(node);
            int index = owner.IndexOf(node);
            if (index == -1)
                return;

            using var bulk = historyHandler.BulkEdit("Wrap condition");
            var wrapper = conditionsFactory.Create(logicalType);
            ApplySourceTarget(wrapper);
            var parent = node.Parent;
            owner.RemoveAt(index);
            node.Parent = wrapper;
            wrapper.Children.Add(node);
            wrapper.Parent = parent;
            wrapper.IsExpanded = true;
            owner.Insert(index, wrapper);
            SelectedCondition = wrapper;
        }

        internal ObservableCollection<MangosConditionViewModel> OwnerCollection(MangosConditionViewModel node) =>
            node.Parent?.Children ?? Roots;

        private MangosConditionViewModel CloneNode(MangosConditionViewModel source)
        {
            var clone = conditionsFactory.Create(source.ToLine());
            clone.SetSourceTarget(sourceTarget);
            clone.OriginalEntry = source.OriginalEntry;
            foreach (var child in source.Children)
            {
                var childClone = CloneNode(child);
                childClone.Parent = clone;
                clone.Children.Add(childClone);
            }
            return clone;
        }

        private MangosConditionViewModel? clipboard;
        private MangosConditionViewModel? Clipboard
        {
            get => clipboard;
            set => SetProperty(ref clipboard, value);
        }

        private string errors = "";
        public string Errors
        {
            get => errors;
            set
            {
                SetProperty(ref errors, value);
                RaisePropertyChanged(nameof(HasErrors));
            }
        }
        public bool HasErrors => errors.Length > 0;

        public ICommand PickCommand { get; }
        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public ICommand AddRootCommand { get; }
        public DelegateCommand AddChildCommand { get; }
        public DelegateCommand RemoveItemCommand { get; }
        public DelegateCommand WrapInAndCommand { get; }
        public DelegateCommand WrapInOrCommand { get; }
        public DelegateCommand WrapInNotCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand CutCommand { get; }
        public DelegateCommand PasteCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public int DesiredWidth => 900;
        public int DesiredHeight => 650;
        public string Title { get; set; } = "Conditions edit";
        public bool Resizeable => true;

        public event Action? CloseCancel;
        public event Action? CloseOk;

        public MangosConditionSerializeResult GenerateResult(uint firstFreeEntry) =>
            MangosConditionTreeCodec.Serialize(Roots, firstFreeEntry);

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not MangosConditionViewModel dragged)
                return;
            if (dropInfo.TargetItem is MangosConditionViewModel target && dragged.Descendants().Contains(target))
                return; // cannot drop into own subtree

            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = DragDropEffects.Move;
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is not MangosConditionViewModel dragged)
                return;

            var target = dropInfo.TargetItem as MangosConditionViewModel;
            if (target != null && dragged.Descendants().Contains(target))
                return;

            using var bulk = historyHandler.BulkEdit("Move condition");
            var owner = OwnerCollection(dragged);
            owner.Remove(dragged);

            if (target is { IsLogical: true } && target.Children.Count < target.MaxChildren)
            {
                dragged.Parent = target;
                target.Children.Add(dragged);
                target.IsExpanded = true;
            }
            else if (target != null)
            {
                // drop on a leaf: become its sibling
                var targetOwner = OwnerCollection(target);
                dragged.Parent = target.Parent;
                targetOwner.Insert(targetOwner.IndexOf(target) + 1, dragged);
            }
            else
            {
                dragged.Parent = null;
                Roots.Add(dragged);
            }

            SelectedCondition = dragged;
        }
    }
}
