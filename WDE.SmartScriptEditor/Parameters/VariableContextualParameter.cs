using System.Collections.Generic;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters
{
    public class VariableContextualParameter : IContextualParameter<long, SmartBaseElement>
    {
        private readonly GlobalVariableType type;
        private readonly string name;

        public bool AllowUnknownItems => true;
        
        private static SmartScriptBase? GetScript(SmartBaseElement element)
        {
            if (element is SmartSource source)
                return source.Parent?.Parent?.Parent;
            if (element is SmartAction action)
                return action.Parent?.Parent;
            if (element is SmartEvent @event)
                return @event.Parent;
            if (element is SmartCondition @condition)
                return @condition.Parent?.Parent;
            return null;
        }
        
        internal VariableContextualParameter(GlobalVariableType type, string name)
        {
            this.type = type;
            this.name = name;
        }
        
        public Dictionary<long, SelectOption>? ItemsForContext(SmartBaseElement context)
        {
            Dictionary<long, SelectOption>? dict = null;
            var script = GetScript(context);
            if (script != null)
            {
                foreach (var c in script.GlobalVariables)
                {
                    if (c.VariableType == type && c.Name != "")
                    {
                        dict ??= new();
                        dict[c.Key] = new SelectOption(c.Name);
                    }
                }   
            }

            return dict;
        }
        
        public static string ToString(long value, SmartBaseElement context, string name, GlobalVariableType type)
        {
            var script = GetScript(context);
            if (script != null)
            {
                foreach (var c in script.GlobalVariables)
                {
                    if (c.VariableType == type && c.Key == value && c.Name != "")
                        return c.Name;
                }   
            }

            return $"{name}\\[{value}\\]";
        }

        public string ToString(long value, SmartBaseElement context)
        {
            return ToString(value, context, name, type);
        }

        public bool HasItems => true;

        public string ToString(long value) => value.ToString();
        public Dictionary<long, SelectOption>? Items => null;
    }
}