using System.Collections.Generic;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters
{
    public class VariableContextualParameter : IContextualParameter<long, SmartScriptBase>
    {
        private readonly GlobalVariableType type;
        private readonly string name;

        internal VariableContextualParameter(GlobalVariableType type, string name)
        {
            this.type = type;
            this.name = name;
        }
        
        public string ToString(long value, SmartScriptBase context)
        {
            foreach (var c in context.GlobalVariables)
            {
                if (c.VariableType == type && c.Key == value && c.Name != "")
                    return c.Name;
            }

            return $"{name}\\[{value}\\]";
        }

        public bool HasItems => false;

        public string ToString(long value)
        {
            return value.ToString();
        }

        public Dictionary<long, SelectOption>? Items => null;
    }
}