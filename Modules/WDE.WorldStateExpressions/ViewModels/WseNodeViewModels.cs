using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;
using WDE.WorldStateExpressions.Models;
using WDE.Common.Parameters;
using WDE.Parameters.Models;

namespace WDE.WorldStateExpressions.ViewModels
{
    internal static class WseParameters
    {
        public static readonly Parameter ValueType = new()
        {
            Items = new Dictionary<long, SelectOption>
            {
                [(long)WseValueType.Constant] = new("Constant", "a literal number"),
                [(long)WseValueType.WorldState] = new("Worldstate", "current value of a worldstate variable"),
                [(long)WseValueType.Function] = new("Function", "server-evaluated function"),
            }
        };

        public static readonly Parameter Operator = new()
        {
            Items = new Dictionary<long, SelectOption>
            {
                [0] = new("(no math)"),
                [1] = new("+"),
                [2] = new("-"),
                [3] = new("*"),
                [4] = new("/"),
                [5] = new("%"),
            }
        };

        public static readonly Parameter Compare = new()
        {
            Items = new Dictionary<long, SelectOption>
            {
                [0] = new("(no comparison)", "the clause is true when the left value is non-zero"),
                [1] = new("="),
                [2] = new("≠"),
                [3] = new("<"),
                [4] = new("≤"),
                [5] = new(">"),
                [6] = new("≥"),
            }
        };

        public static readonly Parameter Logic = new()
        {
            Items = new Dictionary<long, SelectOption>
            {
                [(long)WseLogic.And] = new("AND"),
                [(long)WseLogic.Or] = new("OR"),
                [(long)WseLogic.Xor] = new("XOR"),
            }
        };

        public static readonly Parameter Function = new()
        {
            Items = WorldStateExpressionCodec.FunctionNames
                .Select((name, id) => (name, id))
                .ToDictionary(x => (long)x.id, x => new SelectOption(x.name))
        };
    }

    internal class WseSingleValueViewModel : BindableBase
    {
        public event Action? Changed;

        public ParameterValueHolder<long> Type { get; }
        public ParameterValueHolder<long> Constant { get; }
        public ParameterValueHolder<long> WorldStateId { get; }
        public ParameterValueHolder<long> Function { get; }

        private WseSingleValueViewModel? arg1;
        private WseSingleValueViewModel? arg2;
        public WseSingleValueViewModel? Arg1 => arg1;
        public WseSingleValueViewModel? Arg2 => arg2;

        public bool IsConstant => Type.Value == (long)WseValueType.Constant;
        public bool IsWorldState => Type.Value == (long)WseValueType.WorldState;
        public bool IsFunction => Type.Value == (long)WseValueType.Function;

        private readonly IParameter<long> worldStateParameter;

        public WseSingleValueViewModel(WseSingleValue? model, IParameter<long> worldStateParameter)
        {
            this.worldStateParameter = worldStateParameter;
            model ??= new WseConstant(0);
            Type = new ParameterValueHolder<long>("Type", WseParameters.ValueType, model switch
            {
                WseWorldState => (long)WseValueType.WorldState,
                WseFunctionValue => (long)WseValueType.Function,
                _ => (long)WseValueType.Constant
            });
            Constant = new ParameterValueHolder<long>("Constant", Parameter.Instance, (model as WseConstant)?.Value ?? 0);
            WorldStateId = new ParameterValueHolder<long>("Worldstate", worldStateParameter, (model as WseWorldState)?.WorldStateId ?? 0);
            Function = new ParameterValueHolder<long>("Function", WseParameters.Function, (long)((model as WseFunctionValue)?.FunctionId ?? 0));

            if (model is WseFunctionValue function)
            {
                arg1 = Child(new WseSingleValueViewModel(function.Arg1, worldStateParameter));
                arg2 = Child(new WseSingleValueViewModel(function.Arg2, worldStateParameter));
            }

            Type.OnValueChanged += (_, _, _) =>
            {
                EnsureArgs();
                RaisePropertyChanged(nameof(IsConstant));
                RaisePropertyChanged(nameof(IsWorldState));
                RaisePropertyChanged(nameof(IsFunction));
                Changed?.Invoke();
            };
            Constant.OnValueChanged += (_, _, _) => Changed?.Invoke();
            WorldStateId.OnValueChanged += (_, _, _) => Changed?.Invoke();
            Function.OnValueChanged += (_, _, _) => Changed?.Invoke();
        }

        private WseSingleValueViewModel Child(WseSingleValueViewModel child)
        {
            child.Changed += () => Changed?.Invoke();
            return child;
        }

        private void EnsureArgs()
        {
            if (!IsFunction || arg1 != null)
                return;
            arg1 = Child(new WseSingleValueViewModel(null, worldStateParameter));
            arg2 = Child(new WseSingleValueViewModel(null, worldStateParameter));
            RaisePropertyChanged(nameof(Arg1));
            RaisePropertyChanged(nameof(Arg2));
        }

        public WseSingleValue ToModel()
        {
            if (IsWorldState)
                return new WseWorldState((uint)WorldStateId.Value);
            if (IsFunction)
                return new WseFunctionValue((uint)Function.Value,
                    arg1?.ToModel() ?? new WseConstant(0),
                    arg2?.ToModel() ?? new WseConstant(0));
            return new WseConstant((int)Constant.Value);
        }
    }

    internal class WseValueViewModel : BindableBase
    {
        public event Action? Changed;

        public WseSingleValueViewModel Left { get; }
        public ParameterValueHolder<long> Operator { get; }

        private WseSingleValueViewModel? right;
        public WseSingleValueViewModel? Right => right;
        public bool HasRight => Operator.Value != (long)WseOperator.None;

        private readonly IParameter<long> worldStateParameter;

        public WseValueViewModel(WseValue? model, IParameter<long> worldStateParameter)
        {
            this.worldStateParameter = worldStateParameter;
            Left = Child(new WseSingleValueViewModel(model?.Left, worldStateParameter));
            Operator = new ParameterValueHolder<long>("Operator", WseParameters.Operator, (long)(model?.Operator ?? WseOperator.None));
            if (model?.Right != null)
                right = Child(new WseSingleValueViewModel(model.Right, worldStateParameter));

            Operator.OnValueChanged += (_, _, _) =>
            {
                EnsureRight();
                RaisePropertyChanged(nameof(HasRight));
                Changed?.Invoke();
            };
        }

        private WseSingleValueViewModel Child(WseSingleValueViewModel child)
        {
            child.Changed += () => Changed?.Invoke();
            return child;
        }

        private void EnsureRight()
        {
            if (!HasRight || right != null)
                return;
            right = Child(new WseSingleValueViewModel(null, worldStateParameter));
            RaisePropertyChanged(nameof(Right));
        }

        public WseValue ToModel()
        {
            var op = (WseOperator)Operator.Value;
            if (op == WseOperator.None)
                return new WseValue(Left.ToModel());
            return new WseValue(Left.ToModel(), op, right?.ToModel() ?? new WseConstant(0));
        }
    }

    internal class WseClauseViewModel : BindableBase
    {
        public event Action? Changed;

        public ParameterValueHolder<long> LogicToPrevious { get; }
        public WseValueViewModel Left { get; }
        public ParameterValueHolder<long> Compare { get; }

        private WseValueViewModel? right;
        public WseValueViewModel? Right => right;
        public bool HasRight => Compare.Value != (long)WseCompare.None;

        private bool isFirst;
        public bool IsFirst
        {
            get => isFirst;
            set => SetProperty(ref isFirst, value);
        }

        private readonly IParameter<long> worldStateParameter;

        public WseClauseViewModel(IParameter<long> worldStateParameter, WseClause? model = null, WseLogic logicToPrevious = WseLogic.And)
        {
            this.worldStateParameter = worldStateParameter;
            LogicToPrevious = new ParameterValueHolder<long>("Logic", WseParameters.Logic,
                (long)(logicToPrevious == WseLogic.None ? WseLogic.And : logicToPrevious));
            Left = Child(new WseValueViewModel(model?.Left, worldStateParameter));
            Compare = new ParameterValueHolder<long>("Compare", WseParameters.Compare, (long)(model?.Compare ?? WseCompare.None));
            if (model?.Right != null)
                right = Child(new WseValueViewModel(model.Right, worldStateParameter));

            LogicToPrevious.OnValueChanged += (_, _, _) => Changed?.Invoke();
            Compare.OnValueChanged += (_, _, _) =>
            {
                EnsureRight();
                RaisePropertyChanged(nameof(HasRight));
                Changed?.Invoke();
            };
        }

        private WseValueViewModel Child(WseValueViewModel child)
        {
            child.Changed += () => Changed?.Invoke();
            return child;
        }

        private void EnsureRight()
        {
            if (!HasRight || right != null)
                return;
            right = Child(new WseValueViewModel(null, worldStateParameter));
            RaisePropertyChanged(nameof(Right));
        }

        public WseClause ToModel()
        {
            var compare = (WseCompare)Compare.Value;
            if (compare == WseCompare.None)
                return new WseClause(Left.ToModel());
            return new WseClause(Left.ToModel(), compare, right?.ToModel() ?? new WseValue(new WseConstant(0)));
        }
    }
}
