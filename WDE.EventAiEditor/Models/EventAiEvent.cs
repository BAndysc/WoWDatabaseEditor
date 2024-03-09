using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Parameters.Models;

namespace WDE.EventAiEditor.Models
{
    public class EventAiEvent : EventAiBaseElement
    {
        public static readonly int EventParamsCount = 6;

        private bool isSelected;
        private ParameterValueHolder<long> chance;
        private ParameterValueHolder<long> flags;
        private ParameterValueHolder<long> phases;

        public EventAiBase? Parent { get; set; }

        public EventAiEvent(uint id) : base(EventParamsCount, id, that => new ConstContextParameterValueHolder<long, EventAiBaseElement>(Parameter.Instance, 0, that))
        {
            Actions = new ObservableCollection<EventAiAction>();

            Actions.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (EventAiAction ob in args.NewItems!)
                        ob.Parent = this;
                }
            };

            flags = new ParameterValueHolder<long>("Flags", EventFlagParameter.Instance, 0);
            chance = new ParameterValueHolder<long>("Chance", Parameter.Instance, 0);
            phases = new ParameterValueHolder<long>("Ignore in phases", EventAiPhaseMaskParameter.Instance, 0);

            flags.PropertyChanged += (_, _) => CallOnChanged();
            chance.PropertyChanged += (_, _) =>
            {
                CallOnChanged();
                OnPropertyChanged(nameof(ChanceString));
            };
            phases.PropertyChanged += (_, _) => CallOnChanged();
        }

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

        public string? ChanceString => chance.Value == 100 ? null : $"{chance.Value}%";

        public ParameterValueHolder<long> Phases => phases;
        public ParameterValueHolder<long> Chance => chance;
        public ParameterValueHolder<long> Flags => flags;

        public ObservableCollection<EventAiAction> Actions { get; }

        public override string Readable
        {
            get
            {
                string? readable = ReadableHint;
                if (DescriptionRules != null)
                {
                    var context = new EventAiValidationContext(Parent!, this, null);
                    
                    foreach (DescriptionRule rule in DescriptionRules)
                    {
                        if (rule.Matches(context))
                        {
                            readable = rule.Description;
                            break;
                        }
                    }
                }

                if (readable == null)
                    return "(internal error 'Readable' is null, report at github)";
                
                try
                {
                    string output = Smart.Format(readable,
                        new
                        {
                            pram1 = "[p=0]" + GetParameter(0) + "[/p]",
                            pram2 = "[p=1]" + GetParameter(1) + "[/p]",
                            pram3 = "[p=2]" + GetParameter(2) + "[/p]",
                            pram4 = "[p=3]" + GetParameter(3) + "[/p]",
                            pram5 = "[p=4]" + GetParameter(4) + "[/p]",
                            pram6 = "[p=5]" + GetParameter(5) + "[/p]",
                            pram1value = GetParameter(0).Value,
                            pram2value = GetParameter(1).Value,
                            pram3value = GetParameter(2).Value,
                            pram4value = GetParameter(3).Value,
                            pram5value = GetParameter(4).Value,
                            pram6value = GetParameter(5).Value
                        });
                    return output;
                }
                catch (ParsingErrors e)
                {
                    LOG.LogWarning(e.ToString());
                    return $"Event {Id} has invalid Readable format in events.json";
                }
                catch (FormattingException e)
                {
                    LOG.LogWarning(e.ToString());
                    return $"Event {Id} has invalid Readable format in events.json";
                }
            }
        }

        public void AddAction(EventAiAction eventAiAction)
        {
            Actions.Add(eventAiAction);
        }

        public void InsertAction(EventAiAction eventAiAction, int indexAt)
        {
            Actions.Insert(indexAt, eventAiAction);
        }

        public bool Equals(EventAiEvent other)
        {
            if (Id != other.Id)
                return false;
            
            for (int i = 0; i < EventParamsCount; ++i)
            {
                if (GetParameter(i).Value != other.GetParameter(i).Value)
                    return false;
            }

            if (Flags.Value != other.Flags.Value)
                return false;

            if (Phases.Value != other.Phases.Value)
                return false;

            return Chance.Value == other.Chance.Value;
        }

        public EventAiEvent DeepCopy()
        {
            var shallow = ShallowCopy();

            foreach (var action in Actions)
                shallow.AddAction(action.Copy());
            
            return shallow;
        }

        public EventAiEvent ShallowCopy()
        {
            EventAiEvent se = new(Id);
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            se.Chance.Value = Chance.Value;
            se.Flags.Value = Flags.Value;
            se.Chance.Value = Chance.Value;
            se.Phases.Value = Phases.Value;
            for (var i = 0; i < EventParamsCount; ++i)
            {
                se.GetParameter(i).Copy(GetParameter(i));
            }

            return se;
        }

        public override void InvalidateReadable()
        {
            base.InvalidateReadable();
            foreach (var a in Actions)
                a.InvalidateReadable();
        }
    }

    [Flags]
    public enum EventChangedMask
    {
        EventValues = 1,
        ActionsValues = 2,
        Actions = 4,
        Script = 8,
        Event = EventValues | Actions
    }
    
    public enum EventAiFlag
    {
        [Description("Event repeats (Does not repeat if this flag is not set)")]
        Repeatable = 1,

        [Description("Event only occurs in Normal instance difficulty + [wotlk: (10-Man Normal)]")]
        Difficulty0 = 2,

        [Description("Event only occurs in Heroic instance difficulty + [wotlk: (25-Man Normal)]")]
        Difficulty1 = 4,

        [Description("Event only occurs in [wotlk: (10-Man Heroic)]")]
        Difficulty2 = 8,

        [Description("Event only occurs in [wotlk (25-Man Heroic)]")]
        Difficulty3 = 16,

        [Description("Random use action1, 2, or 3")]
        RandomAction = 32,

        [Description("Reserved")]
        Reserved6 = 64,

        [Description("Event only occurs in debug build")]
        DebugOnly = 128,

        [Description("Event only occurs in ranged mode")]
        RangedModeOnly = 256,

        [Description("Event only occurs in melee mode")]
        MeleeModeOnly = 512,

        [Description("Only one per cycle")]
        CombatAction = 1024,
    }

    public enum EventAiPhases
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
        Phase12 = 2048,
        Phase13 = 4096,
        Phase14 = 8192,
        Phase15 = 16384,
        Phase16 = 32768,
        Phase17 = 65536,
        Phase18 = 131072,
        Phase19 = 262144,
        Phase20 = 524288,
        Phase21 = 1048576,
        Phase22 = 2097152,
        Phase23 = 4194304,
        Phase24 = 8388608,
        Phase25 = 16777216,
        Phase26 = 33554432,
        Phase27 = 67108864,
        Phase28 = 134217728,
        Phase29 = 268435456,
        Phase30 = 536870912,
        Phase31 = 1073741824
    }

    public class EventFlagParameter : FlagParameter
    {
        public new static EventFlagParameter Instance { get; } = new EventFlagParameter();

        public EventFlagParameter()
        {
            Items = Enum
                .GetValues<EventAiFlag>()
                .ToDictionary(f => (long) f, f => new SelectOption(f.ToString(), f.GetDescription()));
        }
    }

    public static class EnumExtensions
    {
        public static string? GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val)!);
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

    public class EventAiPhaseMaskParameter : FlagParameter
    {
        public new static EventAiPhaseMaskParameter Instance { get; } = new EventAiPhaseMaskParameter();
        public EventAiPhaseMaskParameter()
        {
            Items = Enum
                .GetValues<EventAiPhases>()
                .ToDictionary(f => (long) f, f => new SelectOption(f.ToString()));
        }
    }

    public class EventAiPhaseParameter : Parameter
    {
        public new static EventAiPhaseParameter Instance { get; } = new EventAiPhaseParameter();
        public EventAiPhaseParameter()
        {
            Items = Enumerable.Range(0, 32)
                .ToDictionary(o => (long)o, o => new SelectOption("Phase " + o));
        }
    }
}
