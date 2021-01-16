using System;
using System.ComponentModel;
using SmartFormat;
using SmartFormat.Core.Parsing;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartAction : SmartBaseElement
    {
        public static readonly int SmartActionParametersCount = 6;
        
        private bool isSelected;

        private SmartEvent parent;

        private SmartSource source;

        private SmartTarget target;

        private StringParameter comment;

        public SmartAction(int id, SmartSource source, SmartTarget target) : base(SmartActionParametersCount, id)
        {
            if (source == null || target == null)
                throw new ArgumentNullException("Source or target is null");

            Source = source;
            Target = target;
            comment = new StringParameter("Comment");
            comment.OnValueChanged += (sender, o) => CallOnChanged();
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

        public SmartSource Source
        {
            get => source;
            set
            {
                if (source != null)
                    source.OnChanged -= SourceOnOnChanged;
                source = value;
                source.OnChanged += SourceOnOnChanged;
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

        public StringParameter CommentParameter => comment;
        
        public SmartTarget Target
        {
            get => target;
            set
            {
                if (target != null)
                    target.OnChanged -= SourceOnOnChanged;
                target = value;
                target.OnChanged += SourceOnOnChanged;
            }
        }

        public override string Readable
        {
            get
            {
                try
                {
                    string output = Smart.Format(ReadableHint,
                        new
                        {
                            target = Target.Readable,
                            source = Source.Readable,
                            targetcoords = Target.GetCoords(),
                            target_position = Target.GetPosition(),
                            targetid = Target.Id,
                            pram1 = GetParameter(0),
                            pram2 = GetParameter(1),
                            pram3 = GetParameter(2),
                            pram4 = GetParameter(3),
                            pram5 = GetParameter(4),
                            pram6 = GetParameter(5),
                            datapram1 = "data #" + GetParameter(0).GetValue(),
                            stored = "stored target #" + GetParameter(0).GetValue(),
                            storedPoint = "stored point #" + GetParameter(0).GetValue(),
                            timed1 = "timed event #" + GetParameter(0).GetValue(),
                            timed4 = "timed event #" + GetParameter(0).GetValue(),
                            function1 = "function #" + GetParameter(0).GetValue(),
                            action1 = "action #" + GetParameter(0).GetValue(),
                            pram1value = GetParameter(0).GetValue(),
                            pram2value = GetParameter(1).GetValue(),
                            pram3value = GetParameter(2).GetValue(),
                            pram4value = GetParameter(3).GetValue(),
                            pram5value = GetParameter(4).GetValue(),
                            pram6value = GetParameter(5).GetValue(),
                            comment = Comment
                        });
                    return output;
                }
                catch (ParsingErrors)
                {
                    return $"Action {Id} has invalid Readable format in actions.json";
                }
            }
        }

        public override int ParametersCount => 6;

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
            se.Comment = Comment;
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            for (var i = 0; i < ParametersCount; ++i)
                se.SetParameterObject(i, GetParameter(i).Clone());
            return se;
        }
    }
}