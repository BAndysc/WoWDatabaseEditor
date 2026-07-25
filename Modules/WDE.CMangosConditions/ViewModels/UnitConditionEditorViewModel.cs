using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using WDE.CMangosConditions.Data;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.Parameters.Models;

namespace WDE.CMangosConditions.ViewModels
{
    [AutoRegister]
    internal class UnitConditionEditorViewModel : ObservableBase, IDialog
    {
        private static readonly Parameter LogicParameter = new()
        {
            Items = new Dictionary<long, SelectOption>
            {
                [0] = new("AND", "all clauses must match"),
                [1] = new("OR", "any clause matches"),
            }
        };

        private readonly IUnitConditionClauseFactory clauseFactory;
        private readonly UnitConditionHistoryHandler historyHandler;
        private readonly uint preservedFlags;

        public UnitConditionEditorViewModel(
            IUnitConditionDataManager dataManager,
            IUnitConditionClauseFactory clauseFactory,
            IParameterPickerService parameterPickerService,
            IHistoryManager historyManager,
            IMangosUnitConditionLine line)
        {
            this.clauseFactory = clauseFactory;
            HistoryManager = historyManager;

            Variables = dataManager.AllVariables.ToList();
            preservedFlags = line.Flags & ~1u;

            Id = new ParameterValueHolder<long>("Id", Parameter.Instance, line.Id);
            IsOr = new ParameterValueHolder<long>("Logic", LogicParameter, line.Flags & 1);
            IsOr.OnValueChanged += (_, _, _) => RaisePropertyChanged(nameof(Readable));

            for (int i = 0; i < IMangosUnitConditionLine.ClausesCount; ++i)
            {
                var clause = line.GetClause(i);
                if (!clause.IsNone)
                    Clauses.Add(Watched(clauseFactory.Create(clause)));
            }

            Accept = new DelegateCommand(() =>
            {
                Errors = string.Join("\n", Validate());
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

            AddClauseCommand = new DelegateCommand(() =>
            {
                Clauses.Add(Watched(clauseFactory.CreateDefault()));
            }, () => Clauses.Count < IMangosUnitConditionLine.ClausesCount);

            RemoveClauseCommand = new DelegateCommand<UnitConditionClauseViewModel>(clause =>
            {
                if (clause != null)
                    Clauses.Remove(clause);
            });

            Clauses.CollectionChanged += (_, _) =>
            {
                AddClauseCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(Readable));
            };

            UndoCommand = new DelegateCommand(historyManager.Undo, () => historyManager.CanUndo)
                .ObservesProperty(() => HistoryManager.CanUndo);
            RedoCommand = new DelegateCommand(historyManager.Redo, () => historyManager.CanRedo)
                .ObservesProperty(() => HistoryManager.CanRedo);

            historyHandler = AutoDispose(new UnitConditionHistoryHandler(this, clauseFactory));
            HistoryManager.AddHandler(historyHandler);
        }

        private UnitConditionClauseViewModel Watched(UnitConditionClauseViewModel clause)
        {
            clause.PropertyChanged += OnClauseChanged;
            return clause;
        }

        private void OnClauseChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UnitConditionClauseViewModel.Readable))
                RaisePropertyChanged(nameof(Readable));
        }

        public ObservableCollection<UnitConditionClauseViewModel> Clauses { get; } = new();
        public IList<UnitConditionVariableJson> Variables { get; }
        public IHistoryManager HistoryManager { get; }

        public ParameterValueHolder<long> Id { get; }
        public ParameterValueHolder<long> IsOr { get; }

        public string Readable
        {
            get
            {
                if (Clauses.Count == 0)
                    return "(no clauses — always true)";
                var separator = IsOr.Value != 0 ? " OR " : " AND ";
                return string.Join(separator, Clauses.Select(c => c.Readable));
            }
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Id.Value == 0)
                errors.Add("Id 0 is not a valid unit condition id.");
            if (Id.Value == -1)
                errors.Add("Id -1 is reserved as the \"no condition\" value in consumer tables.");
            if (Id.Value < int.MinValue || Id.Value > int.MaxValue)
                errors.Add("Id must fit in a signed 32 bit integer.");
            return errors;
        }

        public AbstractMangosUnitConditionLine ToLine()
        {
            var result = new AbstractMangosUnitConditionLine
            {
                Id = (int)Id.Value,
                Flags = preservedFlags | (IsOr.Value != 0 ? 1u : 0u),
            };
            for (int i = 0; i < Clauses.Count; ++i)
                result.SetClause(i, Clauses[i].ToClause());
            return result;
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
        public DelegateCommand AddClauseCommand { get; }
        public DelegateCommand<UnitConditionClauseViewModel> RemoveClauseCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public int DesiredWidth => 750;
        public int DesiredHeight => 600;
        public string Title { get; set; } = "Unit condition edit";
        public bool Resizeable => true;

        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}
