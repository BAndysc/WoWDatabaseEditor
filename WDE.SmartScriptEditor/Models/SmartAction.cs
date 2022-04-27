using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartAction : SmartBaseElement
    {
        public static readonly int SmartActionParametersCount = 6;
        
        private bool isSelected;

        private SmartEvent? parent;

        private SmartSource source;

        private SmartTarget target;

        private ParameterValueHolder<string> comment;

        public event Action<SmartAction, IList<ICondition>?, IList<ICondition>?> OnConditionsChanged = delegate { };
        
        public SmartAction(int id, SmartSource source, SmartTarget target) : 
            base(SmartActionParametersCount, id, that => new SmartScriptParameterValueHolder(Parameter.Instance, 0, that))
        {
            if (source == null || target == null)
                throw new ArgumentNullException("Source or target is null");

            this.source = source;
            this.target = target;
            source.Parent = this;
            target.Parent = this;
            source.OnChanged += SourceOnOnChanged;
            target.OnChanged += SourceOnOnChanged;
            comment = new ParameterValueHolder<string>("Comment", StringParameter.Instance, "");
            comment.PropertyChanged += (_, _) =>
            {
                CallOnChanged();
                OnPropertyChanged(nameof(Comment));
            };
            
            Context.Add(new MetaSmartSourceTargetEdit(this, true));
            Context.Add(new MetaSmartSourceTargetEdit(this, false));
        }

        private int indent;
        public int Indent
        {
            get => indent;
            set
            {
                indent = value;
                OnPropertyChanged();
            }
        }

        public ActionFlags ActionFlags { get; set; }

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

        public SmartSource Source => source;

        private string conditionReadable = "true";

        private IList<ICondition>? conditions;
        public IList<ICondition>? Conditions
        {
            get => conditions;
            set
            {
                var old = conditions;
                conditions = value;
                OnConditionsChanged?.Invoke(this, old, value);
                if (value == null)
                    conditionReadable = "true";
                else
                {
                    conditionReadable = string.Join(" [p]OR[/p] ", value
                        .GroupBy(i => i.ElseGroup)
                        .Select(group => "(" + string.Join(" [p]AND[/p] ", group.Select(g => g.Comment)) + ")"));
                }
                InvalidateReadable();
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

        public string Comment
        {
            get => comment.Value;
            set
            {
                comment.Value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Readable));
            }
        }

        public ParameterValueHolder<string> CommentParameter => comment;

        public SmartTarget Target => target;

        public override string Readable
        {
            get
            {
                try
                {
                    string? readable = ReadableHint;
                    if (DescriptionRules != null)
                    {
                        var context = new SmartValidationContext(Parent!.Parent!, Parent!, this);
                    
                        foreach (DescriptionRule rule in DescriptionRules)
                        {
                            if (rule.Matches(context))
                            {
                                readable = rule.Description;
                                break;
                            }
                        }
                    }
                    
                    string output = Smart.Format(readable,
                        new
                        {
                            target = "[s=7]" + Target.Readable + "[/s]",
                            source = "[s=6]" + Source.Readable + "[/s]",
                            targetcoords = "[p]" + Target.GetCoords() + "[/p]",
                            target_position = "[s=6]" + Target.GetPosition() + "[/s]",
                            targetid = Target.Id,
                            sourceid = Source.Id,
                            pram2_m1 = "[p=1]" + (GetParameter(1).Value - 1) + "[/p]",
                            condition1 = conditionReadable,
                            hascondition = (conditions?.Count ?? 0) > 0,
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
                            pram6value = GetParameter(5).Value,
                            target_x = target.X.ToString(CultureInfo.InvariantCulture),
                            target_y = target.Y.ToString(CultureInfo.InvariantCulture),
                            target_z = target.Z.ToString(CultureInfo.InvariantCulture),
                            target_o = target.O.ToString(CultureInfo.InvariantCulture),
                            comment = Comment
                        });
                    return output;
                }
                catch (ParsingErrors e)
                {
                    Console.WriteLine(e.ToString());
                    return $"Action {Id} has invalid Readable format in actions.json";
                }
                catch (FormattingException e)
                {
                    Console.WriteLine(e.ToString());
                    return $"Action {Id} has invalid Readable format in actions.json";
                }
            }
        }

        private void ParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartEvent.IsSelected))
                //if (!_parent.IsSelected)
                //    IsSelected = false;
                OnPropertyChanged(nameof(IsSelected));
        }

        private void SourceOnOnChanged(object? sender, EventArgs e)
        {
            CallOnChanged();
        }

        public SmartAction Copy()
        {
            SmartAction se = new(Id, Source.Copy(), Target.Copy());
            se.ReadableHint = ReadableHint;
            se.ActionFlags = ActionFlags;
            se.DescriptionRules = DescriptionRules;
            se.Conditions = Conditions?.ToList();
            se.LineId = LineId;
            for (var i = 0; i < SmartActionParametersCount; ++i)
                se.GetParameter(i).Copy(GetParameter(i));
            se.comment.Copy(comment);
            return se;
        }
    }

    public class MetaSmartSourceTargetEdit
    {
        public readonly SmartAction RelatedAction;
        public readonly bool IsSource;

        public MetaSmartSourceTargetEdit(SmartAction relatedAction, bool isSource)
        {
            RelatedAction = relatedAction;
            IsSource = isSource;
        }
    }
}