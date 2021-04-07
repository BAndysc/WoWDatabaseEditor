using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Models;

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
            cooldownMin = new ParameterValueHolder<long>("Cooldown min", Parameter.Instance);
            cooldownMax = new ParameterValueHolder<long>("Cooldown max", Parameter.Instance);

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
                            source_type = (int?) Parent?.SourceType ?? 0
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

    public enum SmartEventFlag
    {
        [Description(
            "The event will be fired only once until next Reset() call. If you do not want to run it again even after Reset, add flag DontReset")]
        NotRepeatable = 1,

        [Description("The event will only be fired in normal dungeon/10 man normal raid")]
        Difficulty0 = 2,

        [Description("The event will only be fired in heroic dungeon/25 man normal raid")]
        Difficulty1 = 4,

        [Description("The event will only be fired in epic dungeon/10 man heroic raid")]
        Difficulty2 = 8,

        [Description("The event will only be fired in 25 man heroic raid")]
        Difficulty3 = 16,

        [Description("The event will only be fired if TrinityCore is compiled with DEBUG flag")]
        DebugOnly = 128,

        [Description("If you set this flag, NotRepeatable will not reset after Reset() call")]
        DontReset = 256,

        [Description("Event will be fired even if NPC is charmed")]
        WhileCharmed = 512
    }

    public enum SmartEventPhases
    {
        Always = 0,
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 4,
        Phase4 = 8,
        Phase5 = 16,
        Phase6 = 32,
        Phase7 = 64,
        Phase8 = 128,
        Phase9 = 256,
        Phase10 = 512,
        Phase11 = 1024,
        Phase12 = 2048
    }

    public class SmartEventFlagParameter : FlagParameter
    {
        public static SmartEventFlagParameter Instance { get; } = new SmartEventFlagParameter();

        public SmartEventFlagParameter()
        {
            Items = Enum
                .GetValues<SmartEventFlag>()
                .ToDictionary(f => (long) f, f => new SelectOption(f.ToString(), f.GetDescription()));
        }
    }

    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }
            return null; // could also return string.Empty
        }
    }

    public class SmartEventPhaseParameter : FlagParameter
    {
        public static SmartEventPhaseParameter Instance { get; } = new SmartEventPhaseParameter();
        public SmartEventPhaseParameter()
        {
            Items = Enum
                .GetValues<SmartEventPhases>()
                .ToDictionary(f => (long) f, f => new SelectOption(f.ToString()));
        }
    }
}
