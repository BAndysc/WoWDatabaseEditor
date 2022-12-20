using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Parameters;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScriptParameterValueHolder : ConstContextParameterValueHolder<long, SmartBaseElement>
    {
        private SmartScriptType? GetScriptType(SmartBaseElement context)
        {
            if (context is SmartSource source)
                return source.Parent?.Parent?.Parent?.SourceType;
            if (context is SmartAction action)
                return action.Parent?.Parent?.SourceType;
            if (context is SmartEvent @event)
                return @event.Parent?.SourceType;
            return null;
        }
        
        public override string ToString<R>(R context)
        {
            if (Value < 0 && context is SmartBaseElement ctx && GetScriptType(ctx) == SmartScriptType.Template)
            {
                return VariableContextualParameter.ToString(-Value, ctx, "TEMPLATE", GlobalVariableType.DataVariable);
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