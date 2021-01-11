using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SmartFormat;
using SmartFormat.Core.Parsing;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartEvent : SmartBaseElement
    {
        public static readonly int SmartEventParamsCount = 4;

        private Parameter chance;
        private Parameter cooldownMax;
        private Parameter cooldownMin;
        private Parameter flags;

        private bool isSelected;
        private Parameter phases;

        public SmartEvent(int id) : base(SmartEventParamsCount, id)
        {
            Actions = new ObservableCollection<SmartAction>();

            Actions.CollectionChanged += (sender, args) =>
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

        public int ActualId { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public Parameter CooldownMax
        {
            get => cooldownMax;
            set
            {
                if (cooldownMax != null)
                    cooldownMax.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                cooldownMax = value;
            }
        }

        public Parameter CooldownMin
        {
            get => cooldownMin;
            set
            {
                if (cooldownMin != null)
                    cooldownMin.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                cooldownMin = value;
            }
        }

        public Parameter Phases
        {
            get => phases;
            set
            {
                if (phases != null)
                    phases.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                phases = value;
            }
        }

        public Parameter Chance
        {
            get => chance;
            set
            {
                if (chance != null)
                    chance.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                chance = value;
            }
        }

        public Parameter Flags
        {
            get => flags;
            set
            {
                if (flags != null)
                    flags.OnValueChanged -= OnParameterChanged;
                value.OnValueChanged += OnParameterChanged;
                flags = value;
            }
        }

        public ObservableCollection<SmartAction> Actions { get; }

        public override string Readable
        {
            get
            {
                var readable = ReadableHint;
                if (DescriptionRules != null)
                    foreach (var rule in DescriptionRules)
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

                try
                {
                    var output = Smart.Format(readable,
                        new
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
                catch (ParsingErrors)
                {
                    return $"Event {Id} has invalid Readable format in events.json";
                }
            }
        }

        public override int ParametersCount => 4;

        private void OnParameterChanged(object sender, ParameterChangedValue<int> e) { CallOnChanged(); }

        public void AddAction(SmartAction smartAction) { Actions.Add(smartAction); }

        public SmartEvent ShallowCopy()
        {
            var se = new SmartEvent(Id);
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            se.Chance = Chance.Clone();
            se.Flags = Flags.Clone();
            se.Chance = Chance.Clone();
            se.Phases = Phases.Clone();
            se.CooldownMin = CooldownMin.Clone();
            se.CooldownMax = CooldownMax.Clone();
            for (var i = 0; i < ParametersCount; ++i)
                se.SetParameterObject(i, GetParameter(i).Clone());
            return se;
        }
    }
}