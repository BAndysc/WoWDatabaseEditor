using SmartFormat;
using System;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartAction : SmartBaseElement
    {
        public static readonly int SmartActionParametersCount = 6;

        public SmartEvent _parent;
        public SmartEvent Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        private SmartSource _source;
        public SmartSource Source
        {
            get { return _source; }
            set
            {
                _source = value;
                _source.OnChanged += (sender, args) => CallOnChanged();
            }
        }

        private SmartTarget _target;
        public SmartTarget Target
        {
            get { return _target; }
            set
            {
                _target = value;
                _target.OnChanged += (sender, args) => CallOnChanged();
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
                    datapram1 = "data #"+ GetParameter(0).GetValue(),
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
        }

        public override int ParametersCount => 6;

    }
}