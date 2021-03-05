using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartEvent : SmartBaseElement
    {
        public static readonly int SmartEventParamsCount = 4;

        private bool isSelected;
        private ParameterValueHolder<long> chance;
        private ParameterValueHolder<long> cooldownMax;
        private ParameterValueHolder<long> cooldownMin;
        private ParameterValueHolder<long> flags;
        private ParameterValueHolder<long> phases;
        
        public SmartScript Parent { get; set; }

        public SmartEvent(int id) : base(SmartEventParamsCount, id)
        {
            Actions = new ObservableCollection<SmartAction>();
            Conditions = new ObservableCollection<SmartCondition>();

            Actions.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (object ob in args.NewItems)
                        (ob as SmartAction).Parent = this;
                }
            };
            Conditions.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (object ob in args.NewItems)
                        (ob as SmartCondition).Parent = this;
                }
            };
            
            flags = new ParameterValueHolder<long>("Flags", SmartEventFlagParameter.Instance);
            chance = new ParameterValueHolder<long>("Chance", Parameter.Instance);
            phases = new ParameterValueHolder<long>("Phases", SmartEventPhaseParameter.Instance);
            cooldownMin = new ParameterValueHolder<long>("CooldownMin", Parameter.Instance);
            cooldownMax = new ParameterValueHolder<long>("CooldownMax", Parameter.Instance);

            flags.PropertyChanged += (_, _) => CallOnChanged();
            chance.PropertyChanged += (_, _) => CallOnChanged();
            phases.PropertyChanged += (_, _) => CallOnChanged();
            cooldownMin.PropertyChanged += (_, _) => CallOnChanged();
            cooldownMax.PropertyChanged += (_, _) => CallOnChanged();
        }

        public int ActualId { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (value == isSelected)
                    return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public ParameterValueHolder<long> CooldownMax => cooldownMax;
        public ParameterValueHolder<long> CooldownMin => cooldownMin;
        public ParameterValueHolder<long> Phases => phases;
        public ParameterValueHolder<long> Chance => chance;
        public ParameterValueHolder<long> Flags => flags;

        public ObservableCollection<SmartAction> Actions { get; }
        public ObservableCollection<SmartCondition> Conditions { get; }

        public override string Readable
        {
            get
            {
                string readable = ReadableHint;
                if (DescriptionRules != null)
                {
                    foreach (DescriptionRule rule in DescriptionRules)
                    {
                        if (rule.Matches(new DescriptionParams
                        {
                            pram1 = GetParameter(0).Value,
                            pram2 = GetParameter(1).Value,
                            pram3 = GetParameter(2).Value,
                            pram4 = GetParameter(3).Value,
                            source_type = (int?)Parent?.SourceType ?? 0
                        }))
                        {
                            readable = rule.Description;
                            break;
                        }
                    }
                }

                try
                {
                    string output = Smart.Format(readable,
                        new
                        {
                            pram1 = "[p=0]" + GetParameter(0) + "[/p]",
                            pram2 = "[p=1]" + GetParameter(1) + "[/p]",
                            pram3 = "[p=2]" + GetParameter(2) + "[/p]",
                            pram4 = "[p=3]" + GetParameter(3) + "[/p]",
                            datapram1 = GetParameter(0),
                            timed1 = GetParameter(0),
                            function1 = GetParameter(0),
                            action1 = GetParameter(0),
                            pram1value = GetParameter(0).Value,
                            pram2value = GetParameter(1).Value,
                            pram3value = GetParameter(2).Value,
                            pram4value = GetParameter(3).Value
                        });
                    return output;
                }
                catch (ParsingErrors e)
                {
                    Console.WriteLine(e.ToString());
                    return $"Event {Id} has invalid Readable format in events.json";
                }
                catch (FormattingException e)
                {
                    Console.WriteLine(e.ToString());
                    return $"Event {Id} has invalid Readable format in events.json";
                }
            }
        }
        
        public void AddAction(SmartAction smartAction)
        {
            Actions.Add(smartAction);
        }

        public SmartEvent ShallowCopy()
        {
            SmartEvent se = new(Id);
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            se.Chance.Value = Chance.Value;
            se.Flags.Value = Flags.Value;
            se.Chance.Value = Chance.Value;
            se.Phases.Value = Phases.Value;
            se.CooldownMin.Value = CooldownMin.Value;
            se.CooldownMax.Value = CooldownMax.Value;
            for (var i = 0; i < SmartEventParamsCount; ++i)
            {
                se.GetParameter(i).Copy(GetParameter(i));
            }
            return se;
        }
    }

    public class SmartEventFlagParameter : FlagParameter
    {
        public static SmartEventFlagParameter Instance { get; } = new SmartEventFlagParameter();
        public SmartEventFlagParameter()
        {
            Items = new Dictionary<long, SelectOption>()
            {
                [1] = new("NOT_REPEATABLE"),
                [2] = new("DIFFICULTY_0"),
                [4] = new("DIFFICULTY_1"),
                [8] = new("DIFFICULTY_2"),
                [16] = new("DIFFICULTY_3"),
                [128] = new("DEBUG_ONLY"),
                [256] = new("DONT_RESET"),
                [512] = new("WHILE_CHARMED"),
            };
        }
    }
    
    public class SmartEventPhaseParameter : FlagParameter
    {
        public static SmartEventPhaseParameter Instance { get; } = new SmartEventPhaseParameter();
        public SmartEventPhaseParameter()
        {
            Items = new Dictionary<long, SelectOption>()
            {
                [0] = new("Always"),
                [1] = new("Phase 1"),
                [2] = new("Phase 2"),
                [4] = new("Phase 3"),
                [8] = new("Phase 4"),
                [16] = new("Phase 5"),
                [32] = new("Phase 6"),
                [64] = new("Phase 7"),
                [128] = new("Phase 8"),
                [256] = new("Phase 9"),
                [512] = new("Phase 10"),
                [1024] = new("Phase 11"),
                [2048] = new("Phase 12"),
            };
        }
    }
}
