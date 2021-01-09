using SmartFormat;
using System;
using System.ComponentModel;
using WDE.SmartScriptEditor.Editor.UserControls;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartAction : SmartBaseElement
    {
        public static readonly int SmartActionParametersCount = 6;

        private SmartEvent _parent;
        public SmartEvent Parent
        {
            get { return _parent; }
            set
            {
                if (_parent != null)
                    _parent.PropertyChanged -= ParentPropertyChanged;
                _parent = value;
                value.PropertyChanged += ParentPropertyChanged;
            }
        }

        private void ParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartEvent.IsSelected))
            {
                //if (!_parent.IsSelected)
                //    IsSelected = false;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private SmartSource _source;
        public SmartSource Source
        {
            get { return _source; }
            set
            {
                if (_source != null)
                    _source.OnChanged -= SourceOnOnChanged;
                _source = value;
                _source.OnChanged += SourceOnOnChanged;
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected || _parent.IsSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        private void SourceOnOnChanged(object? sender, EventArgs e)
        {
            CallOnChanged();
        }

        private SmartTarget _target;
        public SmartTarget Target
        {
            get { return _target; }
            set
            {
                if (_target != null)
                    _target.OnChanged -= SourceOnOnChanged;
                _target = value;
                _target.OnChanged += SourceOnOnChanged;
            }
        }

        public SmartAction(int id, SmartSource source, SmartTarget target) : base(SmartActionParametersCount, id)
        {
            if (source == null || target == null)
                throw new ArgumentNullException("Source or target is null");

            Source = source;
            Target = target;
        }

        public override string Readable
        {
            get
            {
                try
                {
                    string output = Smart.Format(ReadableHint, new
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
                        //comment = Comment
                    });
                    return output;
                }
                catch (SmartFormat.Core.Parsing.ParsingErrors)
                {
                    return $"Action {Id} has invalid Readable format in actions.json";
                }
            }
        }

        public override int ParametersCount => 6;

        public SmartAction Copy()
        {
            SmartAction se = new SmartAction(Id, Source.Copy(), Target.Copy());
            se.ReadableHint = ReadableHint;
            se.DescriptionRules = DescriptionRules;
            for (int i = 0; i < ParametersCount; ++i)
                se.SetParameterObject(i, GetParameter(i).Clone());
            return se;
        }
    }
}