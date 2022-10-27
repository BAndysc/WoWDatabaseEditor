using System;
using System.Collections.Generic;
using System.ComponentModel;
using SmartFormat;
using WDE.Common.Parameters;
using WDE.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models
{
    public enum ConditionTarget
    {
        Invoker = 0,
        Object = 1,
    }

    public class SmartCondition : SmartBaseElement
    {
        private readonly IEditorFeatures features;
        private string comment = "";
        private bool isSelected;
        private SmartEvent? parent;
        private ParameterValueHolder<long> inverted;
        private ParameterValueHolder<long> conditionTarget;

        private string? negativeReadableHint;
        public string? NegativeReadableHint
        {
            get => negativeReadableHint;
            set
            {
                negativeReadableHint = value;
                CallOnChanged(null);
            }
        }
        
        public SmartCondition(int id, bool supportsVictimTarget, IEditorFeatures features) : base(id, features.ConditionParametersCount, that => new ConstContextParameterValueHolder<long, SmartBaseElement>(Parameter.Instance, 0, that))
        {
            this.features = features;
            var conditionTargetParam = new Parameter();
            conditionTargetParam.Items = new Dictionary<long, SelectOption>() {[0] = new("Action invoker"), [1] = new("Object")};
            if (supportsVictimTarget)
                conditionTargetParam.Items.Add(2, new SelectOption("Victim"));

            inverted = new ParameterValueHolder<long>("Inverted", BoolParameter.Instance, 0);
            conditionTarget = new ParameterValueHolder<long>("Condition target", conditionTargetParam, 0);
            inverted.PropertyChanged += ((sender, value) =>
            {
                CallOnChanged(sender);
                OnPropertyChanged(nameof(IsInverted));
            });
            conditionTarget.PropertyChanged += ((sender, value) => CallOnChanged(sender));
            Context.Add(conditionTarget);
        }
        
        public int Indent { get; set; }

        public SmartEvent? Parent
        {
            get => parent;
            set
            {
                if (parent != null)
                    parent.PropertyChanged -= ParentPropertyChanged;
                parent = value;
                if (value != null)
                    value.PropertyChanged += ParentPropertyChanged;
            }
        }

        public bool IsSelected
        {
            get => isSelected || (parent?.IsSelected ?? false);
            set
            {
                if (value == isSelected)
                    return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsInverted => inverted.Value == 1;

        public ParameterValueHolder<long> Inverted => inverted;
        
        public ParameterValueHolder<long> ConditionTarget => conditionTarget;
        
        public string Comment
        {
            get => comment;
            set
            {
                comment = value;
                OnPropertyChanged();
            }
        }

        public override string Readable
        {
            get {
                string? readable = ReadableHint;
                if (readable == null)
                    return "";
                if (negativeReadableHint != null && inverted.Value != 0)
                    readable = negativeReadableHint;
                bool isNegative = inverted.Value != 0;
                return Smart.Format(readable, new
                {
                    target = "[p=3]" + conditionTarget + "[/p]",
                    pram1 = "[p=0]" + GetParameter(0) + "[/p]",
                    pram2 = "[p=1]" + GetParameter(1) + "[/p]",
                    pram3 = "[p=2]" + GetParameter(2) + "[/p]",
                    pram4 = ParametersCount >= 4 ? "[p=3]" + GetParameter(3) + "[/p]" : "",
                    pram1value = GetParameter(0).Value,
                    pram2value = GetParameter(1).Value,
                    pram3value = GetParameter(2).Value,
                    pram4value = ParametersCount >= 4 ? GetParameter(3).Value : 0,
                    pram1comp = (isNegative, GetParameter(0).Value),
                    pram2comp = (isNegative, GetParameter(1).Value),
                    pram3comp = (isNegative, GetParameter(2).Value),
                    pram4comp = (isNegative, ParametersCount >= 4 ? GetParameter(3).Value : 0),
                    
                    negate = inverted.Value == 0
                });
            }
        }

        private void ParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartEvent.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        public SmartCondition Copy()
        {
            SmartCondition se = new(Id, conditionTarget.Parameter.Items?.Count >= 3, features);
            se.Comment = Comment;
            se.ReadableHint = ReadableHint;
            se.NegativeReadableHint = NegativeReadableHint;
            se.DescriptionRules = DescriptionRules;
            se.inverted.Value = inverted.Value;
            se.conditionTarget.Value = conditionTarget.Value;
            for (var i = 0; i < ParametersCount; ++i)
            {
                se.GetParameter(i).Copy(GetParameter(i));
            }
            return se;
        }
    }
}