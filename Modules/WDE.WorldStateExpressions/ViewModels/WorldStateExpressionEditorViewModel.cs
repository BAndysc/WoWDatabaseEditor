using System;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.WorldStateExpressions.Models;

namespace WDE.WorldStateExpressions.ViewModels
{
    [AutoRegister]
    internal class WorldStateExpressionEditorViewModel : ObservableBase, IDialog
    {
        private bool enabled;
        private string parseWarning = "";
        private string errors = "";

        private readonly IParameter<long> worldStateParameter;

        public WorldStateExpressionEditorViewModel(IParameterFactory parameterFactory, WseExpression expression)
        {
            worldStateParameter = parameterFactory.Factory("WorldStateNameParameter");
            enabled = expression.Enabled;
            for (int i = 0; i < expression.Clauses.Count; ++i)
                Clauses.Add(Watched(new WseClauseViewModel(worldStateParameter, expression.Clauses[i],
                    i > 0 ? expression.Logic[i - 1] : WseLogic.And)));
            if (Clauses.Count == 0)
                Clauses.Add(Watched(new WseClauseViewModel(worldStateParameter)));
            UpdateIsFirst();

            Accept = new DelegateCommand(() =>
            {
                try
                {
                    WorldStateExpressionCodec.Encode(ToExpression());
                }
                catch (WseParseException e)
                {
                    Errors = e.Message;
                    return;
                }
                CloseOk?.Invoke();
            });
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

            AddClauseCommand = new DelegateCommand(() =>
            {
                Clauses.Add(Watched(new WseClauseViewModel(worldStateParameter)));
            });

            RemoveClauseCommand = new DelegateCommand<WseClauseViewModel>(clause =>
            {
                if (clause != null && Clauses.Count > 1)
                    Clauses.Remove(clause);
            });

            Clauses.CollectionChanged += (_, _) =>
            {
                UpdateIsFirst();
                RaiseExpressionChanged();
            };
        }

        private WseClauseViewModel Watched(WseClauseViewModel clause)
        {
            clause.Changed += RaiseExpressionChanged;
            return clause;
        }

        private void UpdateIsFirst()
        {
            for (int i = 0; i < Clauses.Count; ++i)
                Clauses[i].IsFirst = i == 0;
        }

        private void RaiseExpressionChanged()
        {
            RaisePropertyChanged(nameof(Readable));
            RaisePropertyChanged(nameof(HexPreview));
        }

        public ObservableCollection<WseClauseViewModel> Clauses { get; } = new();

        public bool Enabled
        {
            get => enabled;
            set
            {
                SetProperty(ref enabled, value);
                RaiseExpressionChanged();
            }
        }

        public string ParseWarning
        {
            get => parseWarning;
            set
            {
                SetProperty(ref parseWarning, value);
                RaisePropertyChanged(nameof(HasParseWarning));
            }
        }
        public bool HasParseWarning => parseWarning.Length > 0;

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

        public string Readable => WorldStateExpressionCodec.ToReadable(ToExpression(), id =>
            worldStateParameter.Items != null && worldStateParameter.Items.TryGetValue(id, out var option)
                ? option.Name : null);

        public string HexPreview
        {
            get
            {
                try
                {
                    return WorldStateExpressionCodec.Encode(ToExpression());
                }
                catch (WseParseException e)
                {
                    return $"(invalid: {e.Message})";
                }
            }
        }

        public WseExpression ToExpression()
        {
            var expression = new WseExpression
            {
                Enabled = Enabled,
                // always emit the trailing logic terminator: without it the cmangos
                // parser reads past the buffer end on multi-clause expressions
                HasTrailingTerminator = true
            };
            for (int i = 0; i < Clauses.Count; ++i)
            {
                expression.Clauses.Add(Clauses[i].ToModel());
                if (i > 0)
                    expression.Logic.Add((WseLogic)Clauses[i].LogicToPrevious.Value);
            }
            return expression;
        }

        public string ToHex() => WorldStateExpressionCodec.Encode(ToExpression());

        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public DelegateCommand AddClauseCommand { get; }
        public DelegateCommand<WseClauseViewModel> RemoveClauseCommand { get; }

        public int DesiredWidth => 900;
        public int DesiredHeight => 650;
        public string Title { get; set; } = "Worldstate expression edit";
        public bool Resizeable => true;

        public event Action? CloseCancel;
        public event Action? CloseOk;
    }
}
