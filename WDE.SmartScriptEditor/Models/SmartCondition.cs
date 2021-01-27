using System;
using System.Collections.Generic;
using System.ComponentModel;
using SmartFormat;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public enum ConditionTarget
    {
        Invoker = 0,
        Object = 1,
    }

    public class SmartCondition : SmartBaseElement
    {
        public static readonly int SmartConditionParametersCount = 3;

        private string comment;
        private bool isSelected;
        private SmartEvent parent;
        private ParameterValueHolder<int> inverted;
        private ParameterValueHolder<int> conditionTarget;

        public SmartCondition(int id) : base(SmartConditionParametersCount, id)
        {
            var invertedParam = new Parameter();
            invertedParam.Items = new Dictionary<int, SelectOption>() {[0] = new("False"), [1] = new("True")};
            
            var conditionTargetParam = new Parameter();
            conditionTargetParam.Items = new Dictionary<int, SelectOption>() {[0] = new("Action invoker"), [1] = new("Object")};

            inverted = new ParameterValueHolder<int>("Inverted", invertedParam);
            conditionTarget = new ParameterValueHolder<int>("Condition target", conditionTargetParam);
            inverted.PropertyChanged += ((sender, value) =>
            {
                CallOnChanged();
                OnPropertyChanged(nameof(IsInverted));
            });
            conditionTarget.PropertyChanged += ((sender, value) => CallOnChanged());
            Context.Add(conditionTarget);
        }

        public SmartEvent Parent
        {
            get => parent;
            set
            {
                if (parent != null)
                    parent.PropertyChanged -= ParentPropertyChanged;
                parent = value;
                value.PropertyChanged += ParentPropertyChanged;
            }
        }

        public bool IsSelected
        {
            get => isSelected || parent.IsSelected;
            set
            {
                if (value == isSelected)
                    return;
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsInverted => inverted.Value == 1;

        public ParameterValueHolder<int> Inverted => inverted;
        
        public ParameterValueHolder<int> ConditionTarget => conditionTarget;
        
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
                string readable = ReadableHint;
                if (readable == null)
                    return "";
                return Smart.Format(readable, new
                {
                    target = "[p=3]" + conditionTarget + "[/p]",
                    pram1 = "[p=0]" + GetParameter(0) + "[/p]",
                    pram2 = "[p=1]" + GetParameter(1) + "[/p]",
                    pram3 = "[p=2]" + GetParameter(2) + "[/p]",
                    datapram1 = GetParameter(0).Value,
                    pram1value = GetParameter(0).Value,
                    pram2value = GetParameter(1).Value,
                    pram3value = GetParameter(2).Value,
                });
            }
        }

        private void ParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartEvent.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        private void SourceOnOnChanged(object? sender, EventArgs e)
        {
            CallOnChanged();
        }

        public SmartCondition Copy()
        {
            SmartCondition se = new(Id);
            se.Comment = Comment;
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            se.inverted.Value = inverted.Value;
            se.conditionTarget.Value = conditionTarget.Value;
            for (var i = 0; i < SmartConditionParametersCount; ++i)
            {
                se.GetParameter(i).Copy(GetParameter(i));
            }
            return se;
        }
    }
}