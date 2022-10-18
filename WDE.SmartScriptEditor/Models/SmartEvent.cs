using System;
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
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartEvent : SmartBaseElement
    {
        private readonly IEditorFeatures features;
        private bool isSelected;
        private ParameterValueHolder<long> chance;
        private ParameterValueHolder<long> cooldownMax;
        private ParameterValueHolder<long> cooldownMin;
        private ParameterValueHolder<long> flags;
        private ParameterValueHolder<long> phases;
        private ParameterValueHolder<long> timerId;

        public SmartScriptBase? Parent { get; set; }

        public SmartEvent(int id, IEditorFeatures features) : base(id, 
            features.EventParametersCount,
            that => new SmartScriptParameterValueHolder(Parameter.Instance, 0, that))
        {
            this.features = features;
            Actions = new ObservableCollection<SmartAction>();
            Conditions = new ObservableCollection<SmartCondition>();

            Actions.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (SmartAction ob in args.NewItems!)
                        ob.Parent = this;
                }
            };
            Conditions.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (SmartCondition ob in args.NewItems!)
                        ob.Parent = this;
                }
            };

            flags = new ParameterValueHolder<long>("Flags", SmartEventFlagParameter.Instance, 0);
            chance = new ParameterValueHolder<long>("Chance", Parameter.Instance, 0);
            phases = new ParameterValueHolder<long>("Phases", SmartEventPhaseParameter.Instance, 0);
            timerId = new ParameterValueHolder<long>("Timer id", Parameter.Instance, 0);
            cooldownMin = new ParameterValueHolder<long>("Cooldown min", Parameter.Instance, 0);
            cooldownMax = new ParameterValueHolder<long>("Cooldown max", Parameter.Instance, 0);

            flags.PropertyChanged += (_, _) => CallOnChanged();
            timerId.PropertyChanged += (_, _) => CallOnChanged();
            chance.PropertyChanged += (_, _) =>
            {
                CallOnChanged();
                OnPropertyChanged(nameof(ChanceString));
            };
            phases.PropertyChanged += (_, _) => CallOnChanged();
            cooldownMin.PropertyChanged += (_, _) => CallOnChanged();
            cooldownMax.PropertyChanged += (_, _) => CallOnChanged();
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

        public ParameterValueHolder<long> CooldownMax => cooldownMax;
        public ParameterValueHolder<long> CooldownMin => cooldownMin;
        public ParameterValueHolder<long> Phases => phases;
        public ParameterValueHolder<long> Chance => chance;
        public ParameterValueHolder<long> Flags => flags;
        public ParameterValueHolder<long> TimerId => timerId;

        public ObservableCollection<SmartAction> Actions { get; }
        public ObservableCollection<SmartCondition> Conditions { get; }

        public override string Readable
        {
            get
            {
                string? readable = ReadableHint;
                if (DescriptionRules != null)
                {
                    var context = new SmartValidationContext(Parent!, this, null);
                    
                    foreach (DescriptionRule rule in DescriptionRules)
                    {
                        if (rule.Matches(context))
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
                            pram5 = ParametersCount >= 5 ? "[p=4]" + GetParameter(4) + "[/p]" : "",
                            fpram1 = FloatParametersCount >= 1 ? "[p]" + GetFloatParameter(0) + "[/p]" : "",
                            fpram2 = FloatParametersCount >= 2 ? "[p]" + GetFloatParameter(1) + "[/p]" : "",
                            spram1 = StringParametersCount >= 1 ? "[p]" + GetStringParameter(0) + "[/p]" : "",
                            pram1value = GetParameter(0).Value,
                            pram2value = GetParameter(1).Value,
                            pram3value = GetParameter(2).Value,
                            pram4value = GetParameter(3).Value,
                            pram5value = ParametersCount >= 5 ? GetParameter(4).Value : 0,
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

        public IEditorFeatures EditorFeatures => features;

        public void AddAction(SmartAction smartAction)
        {
            Actions.Add(smartAction);
        }

        public void InsertAction(SmartAction smartAction, int indexAt)
        {
            Actions.Insert(indexAt, smartAction);
        }

        public bool Equals(SmartEvent other)
        {
            if (Id != other.Id)
                return false;
            
            for (int i = 0; i < ParametersCount; ++i)
            {
                if (GetParameter(i).Value != other.GetParameter(i).Value)
                    return false;
            }
            
            for (int i = 0; i < FloatParametersCount; ++i)
            {
                if (Math.Abs(GetFloatParameter(i).Value - other.GetFloatParameter(i).Value) > 0.00001f)
                    return false;
            }
            
            for (int i = 0; i < StringParametersCount; ++i)
            {
                if (GetStringParameter(i).Value != other.GetStringParameter(i).Value)
                    return false;
            }

            if (Flags.Value != other.Flags.Value)
                return false;

            if (TimerId.Value != other.TimerId.Value)
                return false;

            if (Phases.Value != other.Phases.Value)
                return false;

            if (CooldownMin.Value != other.CooldownMin.Value)
                return false;
            
            if (CooldownMax.Value != other.CooldownMax.Value)
                return false;

            return Chance.Value == other.Chance.Value;
        }

        public SmartEvent DeepCopy()
        {
            var shallow = ShallowCopy();

            foreach (var action in Actions)
                shallow.AddAction(action.Copy());
            
            foreach (var condition in Conditions)
                shallow.Conditions.Add(condition.Copy());
            
            return shallow;
        }

        public SmartEvent ShallowCopy()
        {
            SmartEvent se = new(Id, features);
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            se.Chance.Value = Chance.Value;
            se.Flags.Value = Flags.Value;
            se.Chance.Value = Chance.Value;
            se.Phases.Value = Phases.Value;
            se.CooldownMin.Value = CooldownMin.Value;
            se.CooldownMax.Value = CooldownMax.Value;
            se.TimerId.Value = TimerId.Value;
            se.CopyParameters(this);

            return se;
        }

        public override void InvalidateReadable()
        {
            base.InvalidateReadable();
            foreach (var a in Actions)
                a.InvalidateReadable();
            foreach (var c in Conditions)
                c.InvalidateReadable();
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
        public new static SmartEventFlagParameter Instance { get; } = new SmartEventFlagParameter();

        public SmartEventFlagParameter()
        {
            Items = Enum
                .GetValues<SmartEventFlag>()
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

    public class SmartEventPhaseParameter : FlagParameter
    {
        public new static SmartEventPhaseParameter Instance { get; } = new SmartEventPhaseParameter();
        public SmartEventPhaseParameter()
        {
            Items = Enum
                .GetValues<SmartEventPhases>()
                .ToDictionary(f => (long) f, f => new SelectOption(f.ToString()));
        }
    }
}
