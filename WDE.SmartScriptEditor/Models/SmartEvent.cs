using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SmartFormat;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartEvent : SmartBaseElement
    {
        public static readonly int SmartEventParamsCount = 4;

        private ObservableCollection<SmartAction> _actions;
        private Parameter _flags;
        private Parameter _chance;
        private Parameter _phases;
        private Parameter _cooldownMin;
        private Parameter _cooldownMax;

        public int ActualId { get; set; }

        public Parameter CooldownMax
        {
            get { return _cooldownMax; }
            set
            {
                if (_cooldownMax != null)
                    _cooldownMax.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                _cooldownMax = value;
            }
        }

        public Parameter CooldownMin
        {
            get { return _cooldownMin; }
            set
            {
                if (_cooldownMin != null)
                    _cooldownMin.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                _cooldownMin = value;
            }
        }

        public Parameter Phases
        {
            get { return _phases; }
            set
            {
                if (_phases != null)
                    _phases.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                _phases = value;
            }
        }

        public Parameter Chance
        {
            get { return _chance; }
            set
            {
                if (_chance != null)
                    _chance.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                _chance = value;
            }
        }

        public Parameter Flags
        {
            get { return _flags; }
            set
            {
                if (_flags != null)
                    _flags.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                _flags = value;
            }
        }

        public SmartEvent(int id) : base(SmartEventParamsCount, id)
        {
            _actions = new ObservableCollection<SmartAction>();

            _actions.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                    foreach (var ob in args.NewItems)
                        (ob as SmartAction).Parent = this;
            };

            Flags = new Parameter("Flags");
            Chance = new Parameter("Chance");
            Phases = new Parameter("Phases");
            CooldownMin = new Parameter("CooldownMin");
            CooldownMax = new Parameter("CooldownMax");
        }

        private void OnParameterChanged(object sender, ParameterChangedValue<int> e)
        {
            CallOnChanged();
        }

        public ObservableCollection<SmartAction> Actions => _actions;

        public void AddAction(SmartAction smartAction)
        {
            _actions.Add(smartAction);
        }

        public override string Readable
        {
            get
            {
                string readable = ReadableHint;
                if (DescriptionRules != null)
                {
                    foreach (DescriptionRule rule in DescriptionRules)
                        if (rule.Matches(new DescriptionParams
                        {
                            pram1 = GetParameter(0).GetValue(),
                            pram2 = GetParameter(1).GetValue(),
                            pram3 = GetParameter(2).GetValue(),
                            pram4 = GetParameter(3).GetValue()
                        }))
                        {
                            readable = rule.Description;
                            break;
                        }
                }

                string output = Smart.Format(readable, new
                {
                    pram1 = GetParameter(0),
                    pram2 = GetParameter(1),
                    pram3 = GetParameter(2),
                    pram4 = GetParameter(3),
                    datapram1 = GetParameter(0),
                    timed1 = GetParameter(0),
                    function1 = GetParameter(0),
                    action1 = GetParameter(0),
                    pram1value = GetParameter(0).GetValue(),
                    pram2value = GetParameter(1).GetValue(),
                    pram3value = GetParameter(2).GetValue(),
                    pram4value = GetParameter(3).GetValue()
                });
                return output;
            }
        }

        public SmartEvent ShallowCopy()
        {
            SmartEvent se = new SmartEvent(Id);
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            se.Chance = Chance.Clone();
            se.Flags = Flags.Clone();
            se.Chance = Chance.Clone();
            se.Phases = Phases.Clone();
            se.CooldownMin = CooldownMin.Clone();
            se.CooldownMax = CooldownMax.Clone();
            for (int i = 0; i < ParametersCount; ++i)
                se.SetParameterObject(i, GetParameter(i).Clone());
            return se;
        }
        
        public override int ParametersCount => 4;
    }
}
