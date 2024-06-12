using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Parsing;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models.Helpers;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartAction : VisualSmartBaseElement
    {
        private bool isSelected;

        private SmartEvent? parent;

        private readonly IEditorFeatures features;
        private SmartSource source;

        private SmartTarget target;

        private ParameterValueHolder<string> comment;

        public event Action<SmartAction, IList<ICondition>?, IList<ICondition>?> OnConditionsChanged = delegate { };
        
        public SmartAction(int id, IEditorFeatures features, SmartSource source, SmartTarget target) : 
            base(id,
                features.ActionParametersCount,
                that => new SmartScriptParameterValueHolder(Parameter.Instance, 0, that))
        {
            if (source == null || target == null)
                throw new ArgumentNullException("Source or target is null");

            this.features = features;
            this.source = source;
            this.target = target;
            source.Parent = this;
            target.Parent = this;
            source.OnChanged += SourceOnOnChanged;
            target.OnChanged += SourceOnOnChanged;
            comment = new ParameterValueHolder<string>("Comment", StringParameter.Instance, "");
            comment.PropertyChanged += (sender, _) =>
            {
                CallOnChanged(sender);
                OnPropertyChanged(nameof(Comment));
            };
            while (Context.Count < 8)
                Context.Add(null);
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

        public override SmartScriptBase? Script => Parent?.Script;

        public override SmartType SmartType => SmartType.SmartAction;

        protected override string ReadableImpl
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
                    
                    if (readable == null)
                        return "(internal error 'Readable' is null, report at github)";
                    
                    string output = Smart.Format(readable,
                        new
                        {
                            target = "[s=9]" + Target.Readable + "[/s]",
                            source = "[s=8]" + Source.Readable + "[/s]",
                            targetcoords = "[p]" + Target.GetCoords() + "[/p]",
                            hascoords = Target.X != 0 || Target.Y != 0 || Target.Z != 0 || Target.O != 0,
                            target_position = "[s=9]" + Target.GetPosition() + "[/s]",
                            targetid = Target.Id,
                            sourceid = Source.Id,
                            pram2_m1 = "[p=1]" + (this.GetValueOrDefault(1) - 1) + "[/p]",
                            //condition1 = conditionReadable,
                            //hascondition = (conditions?.Count ?? 0) > 0,
                            pram1 = "[p=0]" + this.GetTextOrDefault(0) + "[/p]",
                            pram2 = "[p=1]" + this.GetTextOrDefault(1) + "[/p]",
                            pram3 = "[p=2]" + this.GetTextOrDefault(2) + "[/p]",
                            pram4 = "[p=3]" + this.GetTextOrDefault(3) + "[/p]",
                            pram5 = "[p=4]" + this.GetTextOrDefault(4) + "[/p]",
                            pram6 = "[p=5]" + this.GetTextOrDefault(5) + "[/p]",
                            pram7 = "[p=6]" + this.GetTextOrDefault(6) + "[/p]",
                            fpram1 = "[p]" + this.GetFloatTextOrDefault(0) + "[/p]",
                            fpram2 =  "[p]" + this.GetFloatTextOrDefault(1) + "[/p]",
                            fpram1value = this.GetFloatValueOrDefault(0),
                            fpram2value =  this.GetFloatValueOrDefault(1),
                            pram1value = this.GetValueOrDefault(0),
                            pram2value = this.GetValueOrDefault(1),
                            pram3value = this.GetValueOrDefault(2),
                            pram4value = this.GetValueOrDefault(3),
                            pram5value = this.GetValueOrDefault(4),
                            pram6value = this.GetValueOrDefault(5),
                            pram7value = this.GetValueOrDefault(6),
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
                    LOG.LogWarning(e.ToString());
                    return $"Action {Id} has invalid Readable format in actions.json";
                }
                catch (FormattingException e)
                {
                    LOG.LogWarning(e.ToString());
                    return $"Action {Id} has invalid Readable format in actions.json";
                }
            }
        }

        private void ParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartEvent.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        private void SourceOnOnChanged(object? sender, EventArgs e)
        {
            CallOnChanged(sender);
        }
        
        public override void InvalidateAllParameters()
        {
            source.InvalidateAllParameters();
            target.InvalidateAllParameters();
            base.InvalidateAllParameters();
        }

        public SmartAction Copy()
        {
            SmartAction se = new(Id, features, source.Copy(), Target.Copy());
            se.ReadableHint = ReadableHint;
            se.ActionFlags = ActionFlags;
            se.DescriptionRules = DescriptionRules;
            //se.Conditions = Conditions?.ToList();
            se.VirtualLineId = VirtualLineId;
            se.DestinationEventId = DestinationEventId;
            se.DestinationTimedActionListId = DestinationTimedActionListId;
            se.IsInInlineActionList = IsInInlineActionList;
            se.CopyParameters(this);
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