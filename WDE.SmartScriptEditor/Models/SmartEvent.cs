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
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models.Helpers;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartEvent : VisualSmartBaseElement
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
        public PositionSize EventPosition { get; set; }
       
        public bool IsBeginGroup => Id == SmartConstants.EventGroupBegin;
        public bool IsEndGroup => Id == SmartConstants.EventGroupEnd;
        public bool IsGroup => IsBeginGroup || IsEndGroup;
        public bool IsEvent => !IsGroup;
        
        public static SmartEvent NewBeginGroup() => new(SmartConstants.EventGroupBegin, SmartGroupFakeEditorFeatures.Instance)
        {
            ReadableHint = SmartConstants.BeginGroupText + "{spram1value}{spram2value:" + SmartConstants.BeginGroupSeparator + "{spram2value}|}"
        };
        
        public static SmartEvent NewEndGroup() => new(SmartConstants.EventGroupEnd, SmartGroupFakeEditorFeatures.Instance)
        {
            ReadableHint = SmartConstants.EndGroupText
        };

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

            flags = new ParameterValueHolder<long>("Flags", features.EventFlagsParameter, 0);
            chance = new ParameterValueHolder<long>("Chance", Parameter.Instance, 0);
            phases = new ParameterValueHolder<long>("Phases", SmartEventPhaseParameter.Instance, 0);
            timerId = new ParameterValueHolder<long>("Timer id", Parameter.Instance, 0);
            cooldownMin = new ParameterValueHolder<long>("Cooldown min", Parameter.Instance, 0);
            cooldownMax = new ParameterValueHolder<long>("Cooldown max", Parameter.Instance, 0);

            flags.PropertyChanged += (sender, _) => CallOnChanged(sender);
            timerId.PropertyChanged += (sender, _) =>
            {
                CallOnChanged(sender);
                OnPropertyChanged(nameof(TimerIdNumber));
            };
            chance.PropertyChanged += (sender, _) =>
            {
                CallOnChanged(sender);
                OnPropertyChanged(nameof(ChanceString));
            };
            phases.PropertyChanged += (sender, _) => CallOnChanged(sender);
            cooldownMin.PropertyChanged += (sender, _) => CallOnChanged(sender);
            cooldownMax.PropertyChanged += (sender, _) => CallOnChanged(sender);
        }

        private SmartEvent? group;
        public SmartGroup? Group
        {
            get => group == null ? null : new SmartGroup(group);
            set
            {
                if (group != null)
                    group.PropertyChanged -= GroupPropertyChanged;
                group = value?.InnerEvent;
                if (group != null)
                    group.PropertyChanged += GroupPropertyChanged;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private void GroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        public bool IsSelected
        {
            get => isSelected || (IsEvent || IsEndGroup) && (group?.IsSelected ?? false);
            set
            {
                if (value == isSelected)
                    return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public long TimerIdNumber => timerId.Value;
        public string? ChanceString => chance.Value == 100 ? null : $"{chance.Value}%";

        public ParameterValueHolder<long> CooldownMax => cooldownMax;
        public ParameterValueHolder<long> CooldownMin => cooldownMin;
        public ParameterValueHolder<long> Phases => phases;
        public ParameterValueHolder<long> Chance => chance;
        public ParameterValueHolder<long> Flags => flags;
        public ParameterValueHolder<long> TimerId => timerId;

        public ObservableCollection<SmartAction> Actions { get; }
        public ObservableCollection<SmartCondition> Conditions { get; }

        public override SmartScriptBase? Script => Parent;

        public override SmartType SmartType => SmartType.SmartEvent;

        protected override string ReadableImpl
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

                if (readable == null)
                    return "(internal error 'Readable' is null, report at github)";
                
                try
                {
                    string output = Smart.Format(readable,
                        new
                        {
                            pram1 = "[p=0]" + this.GetTextOrDefault(0) + "[/p]",
                            pram2 = "[p=1]" + this.GetTextOrDefault(1) + "[/p]",
                            pram3 = "[p=2]" + this.GetTextOrDefault(2) + "[/p]",
                            pram4 = "[p=3]" + this.GetTextOrDefault(3) + "[/p]",
                            pram5 = "[p=4]" + this.GetTextOrDefault(4) + "[/p]",
                            fpram1 = "[p]" + this.GetFloatTextOrDefault(0) + "[/p]",
                            fpram2 = "[p]" + this.GetFloatTextOrDefault(1) + "[/p]",
                            fpram1value = this.GetFloatValueOrDefault(0),
                            fpram2value = this.GetFloatValueOrDefault(1),
                            spram1 = "[p]" + this.GetStringValueOrDefault(0) + "[/p]",
                            spram2 = "[p]" + this.GetStringValueOrDefault(1) + "[/p]",
                            spram1value = this.GetStringValueOrDefault(0),
                            spram2value = this.GetStringValueOrDefault(1),
                            pram1value = this.GetValueOrDefault(0),
                            pram2value = this.GetValueOrDefault(1),
                            pram3value = this.GetValueOrDefault(2),
                            pram4value = this.GetValueOrDefault(3),
                            pram5value = this.GetValueOrDefault(4),
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

        public IEditorFeatures EditorFeatures => features;
        //public bool HasGroup => Group != GroupId.Null;

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

            if (group != other.group)
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

        public SmartEvent CopyWithConditions()
        {
            var shallow = ShallowCopy();

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
            se.DestinationEventId = DestinationEventId;
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
        
        public override void InvalidateAllParameters()
        {
            base.InvalidateAllParameters();
            foreach (var a in Actions)
                a.InvalidateAllParameters();
            foreach (var c in Conditions)
                c.InvalidateAllParameters();
        }
    }

    [Flags]
    public enum EventChangedMask
    {
        EventValues = 1,
        ActionsValues = 2,
        Actions = 4,
        Script = 8,
        Conditions = 16,
        Event = EventValues | Actions | Conditions
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
            Items = new Dictionary<long, SelectOption>()
            {
                [1] = new SelectOption("Not repeatable", "The event will be fired only once until next Reset() call. If you do not want to run it again even after Reset, add flag DontReset"),
                [2] = new SelectOption("Difficulty 0", "The event will only be fired in normal dungeon/10 man normal raid"),
                [4] = new SelectOption("Difficulty 1", "The event will only be fired in heroic dungeon/25 man normal raid"),
                [8] = new SelectOption("Difficulty 2", "The event will only be fired in epic dungeon/10 man heroic raid"),
                [16] = new SelectOption("Difficulty 3", "The event will only be fired in 25 man heroic raid"),
                [128] = new SelectOption("Debug only", "The event will only be fired if TrinityCore is compiled with DEBUG flag"),
                [256] = new SelectOption("Don't reset", "If you set this flag, NotRepeatable will not reset after Reset() call"),
                [512] = new SelectOption("While charmed", "Event will be fired even if NPC is charmed"),
            };
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
