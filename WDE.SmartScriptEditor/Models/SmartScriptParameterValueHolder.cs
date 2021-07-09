using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScriptParameterValueHolder : ConstContextParameterValueHolder<long, SmartBaseElement>
    {
        public override string ToString<R>(R context)
        {
            if (Value < 0 && context is SmartBaseElement ctx)
            {
                return VariableContextualParameter.ToString(-Value, ctx, "data", GlobalVariableType.DataVariable);
            }
            return base.ToString(context);
        }

        public SmartScriptParameterValueHolder(IParameter<long> parameter, long value, SmartBaseElement context) : base(parameter, value, context)
        {
        }

        public SmartScriptParameterValueHolder(IParameter<long> parameter, long value) : base(parameter, value)
        {
        }

        public SmartScriptParameterValueHolder(string name, IParameter<long> parameter, long value) : base(name, parameter, value)
        {
        }
    }
}