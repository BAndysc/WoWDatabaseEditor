using SmartFormat;
using WDE.Common.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartSource : SmartBaseElement
    {
        public static readonly int SmartSourceParametersCount = 3;

        private Parameter condition;

        public SmartSource(int id) : base(SmartSourceParametersCount, id)
        {
            Condition = new Parameter("Condition ID");
        }

        public Parameter Condition
        {
            get => condition;
            set
            {
                if (condition != null)
                    condition.OnValueChanged -= _condition_OnValueChanged;
                condition = value;
                condition.OnValueChanged += _condition_OnValueChanged;
            }
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
                        stored = "Stored target #" + GetParameter(0).GetValue(),
                        storedPoint = "Stored point #" + GetParameter(0).GetValue()
                    });
                return output;
            }
        }

        public override int ParametersCount => 3;

        private void _condition_OnValueChanged(object sender, ParameterChangedValue<int> e)
        {
            CallOnChanged();
        }

        public SmartSource Copy()
        {
            SmartSource se = new(Id) {ReadableHint = ReadableHint, DescriptionRules = DescriptionRules};
            for (var i = 0; i < ParametersCount; ++i)
                se.SetParameterObject(i, GetParameter(i).Clone());
            return se;
        }
    }
}