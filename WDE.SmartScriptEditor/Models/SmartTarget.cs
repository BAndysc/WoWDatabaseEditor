using SmartFormat;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartTarget : SmartSource
    {
        public readonly FloatParameter[] Position;

        public SmartTarget(int id) : base(id)
        {
            Position = new FloatParameter[4];

            Position[0] = new FloatParameter("Target X");
            Position[1] = new FloatParameter("Target Y");
            Position[2] = new FloatParameter("Target Z");
            Position[3] = new FloatParameter("Target O");

            for (var i = 0; i < 4; ++i)
                Position[i].OnValueChanged += (sender, value) => CallOnChanged();
        }

        public bool IsPosition { get; set; }

        public float X
        {
            get => Position[0].GetValue();
            set => Position[0].SetValue(value);
        }

        public float Y
        {
            get => Position[1].GetValue();
            set => Position[1].SetValue(value);
        }

        public float Z
        {
            get => Position[2].GetValue();
            set => Position[2].SetValue(value);
        }

        public float O
        {
            get => Position[3].GetValue();
            set => Position[3].SetValue(value);
        }

        public override string Readable
        {
            get
            {
                string output = Smart.Format(ReadableHint,
                    new
                    {
                        pram1 = GetParameter(0).ToString(),
                        pram2 = GetParameter(1).ToString(),
                        pram3 = GetParameter(2).ToString(),
                        pram1value = GetParameter(0).GetValue(),
                        pram2value = GetParameter(1).GetValue(),
                        pram3value = GetParameter(2).GetValue(),
                        x = X,
                        y = Y,
                        z = Z,
                        o = O,
                        stored = "Stored target #" + GetParameter(0).GetValue(),
                        storedPoint = "Stored point #" + GetParameter(0).GetValue()
                    });
                return output;
            }
        }

        public string GetCoords()
        {
            return $"({X}, {Y}, {Z}, {O})";
        }

        private bool HasPosition()
        {
            for (var i = 0; i < 4; ++i)
            {
                if (Position[i].Value != 0)
                    return true;
            }

            return false;
        }

        public string GetPosition()
        {
            string output = Readable;
            return output.Contains("position") || IsPosition
                ? output
                : "position of " + output + (HasPosition() ? " moved by offset " + GetCoords() : "");
        }

        public SmartTarget Copy()
        {
            SmartTarget se = new(Id)
            {
                ReadableHint = ReadableHint,
                DescriptionRules = DescriptionRules,
                X = X,
                Y = Y,
                Z = Z,
                O = O,
                IsPosition = IsPosition
            };
            for (var i = 0; i < ParametersCount; ++i)
                se.SetParameterObject(i, GetParameter(i).Clone());
            return se;
        }
    }
}