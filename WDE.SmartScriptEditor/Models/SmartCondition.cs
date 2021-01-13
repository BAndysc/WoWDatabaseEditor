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

        private Parameter inverted;
        
        private Parameter conditionTarget;
        
        public SmartCondition(int id) : base(SmartConditionParametersCount, id)
        {
            inverted = new Parameter("Inverted");
            inverted.Items = new Dictionary<int, SelectOption>() {[0] = new("False"), [1] = new("True")};
            
            conditionTarget = new Parameter("Condition target");
            conditionTarget.Items = new Dictionary<int, SelectOption>() {[0] = new("Action invoker"), [1] = new("Object")};
            
            inverted.OnValueChanged += ((sender, value) =>
            {
                CallOnChanged();
                OnPropertyChanged(nameof(IsInverted));
            });
            conditionTarget.OnValueChanged += ((sender, value) => CallOnChanged());
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
                isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool IsInverted => inverted.GetValue() == 1;

        public Parameter Inverted => inverted;
        
        public Parameter ConditionTarget => conditionTarget;

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
                    target = conditionTarget.ToString(),
                    pram1 = GetParameter(0).ToString(),
                    pram2 = GetParameter(1).ToString(),
                    pram3 = GetParameter(2).ToString(),
                    datapram1 = GetParameter(0).GetValue(),
                    pram1value = GetParameter(0).GetValue(),
                    pram2value = GetParameter(1).GetValue(),
                    pram3value = GetParameter(2).GetValue(),
                });
            }
        }

        public override int ParametersCount => SmartConditionParametersCount;

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
            se.inverted.SetValue(inverted.Value);
            se.conditionTarget.SetValue(conditionTarget.Value);
            for (var i = 0; i < ParametersCount; ++i)
                se.SetParameterObject(i, GetParameter(i).Clone());
            return se;
        }
    }
}