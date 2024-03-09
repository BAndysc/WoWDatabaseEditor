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
using WDE.EventAiEditor.Data;

namespace WDE.EventAiEditor.Models
{
    public class EventAiAction : EventAiBaseElement
    {
        public static readonly int ActionParametersCount = 3;
        
        private bool isSelected;

        private EventAiEvent? parent;

        private ParameterValueHolder<string> comment;

        public EventAiAction(uint id) : 
            base(ActionParametersCount, id, that => new ConstContextParameterValueHolder<long, EventAiBaseElement>(Parameter.Instance, 0, that))
        {
            comment = new ParameterValueHolder<string>("Comment", StringParameter.Instance, "");
            comment.PropertyChanged += (_, _) =>
            {
                CallOnChanged();
                OnPropertyChanged(nameof(Comment));
            };
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

        public EventAiEvent? Parent
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

        public override string Readable
        {
            get
            {
                try
                {
                    string? readable = ReadableHint;
                    if (DescriptionRules != null)
                    {
                        var context = new EventAiValidationContext(Parent!.Parent!, Parent!, this);
                    
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
                            source = "[s]" + "Self" + "[/s]",
                            pram1 = "[p=0]" + GetParameter(0) + "[/p]",
                            pram2 = "[p=1]" + GetParameter(1) + "[/p]",
                            pram3 = "[p=2]" + GetParameter(2) + "[/p]",
                            pram1string = GetParameter(0),
                            pram2string = GetParameter(1),
                            pram3string = GetParameter(2),
                            pram1value = GetParameter(0).Value,
                            pram2value = GetParameter(1).Value,
                            pram3value = GetParameter(2).Value,
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
            if (e.PropertyName == nameof(EventAiEvent.IsSelected))
                //if (!_parent.IsSelected)
                //    IsSelected = false;
                OnPropertyChanged(nameof(IsSelected));
        }

        public EventAiAction Copy()
        {
            EventAiAction se = new(Id);
            se.ReadableHint = ReadableHint;
            se.ActionFlags = ActionFlags;
            se.DescriptionRules = DescriptionRules;
            se.LineId = LineId;
            for (var i = 0; i < ActionParametersCount; ++i)
                se.GetParameter(i).Copy(GetParameter(i));
            se.comment.Copy(comment);
            return se;
        }
    }
}