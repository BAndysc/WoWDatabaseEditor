using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters
{
    public class VariableContextualParameter : IContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>
    {
        private readonly GlobalVariableType type;
        private readonly string name;
        private readonly IVariablePickerService picker;
        private readonly IItemFromListProvider itemFromListProvider;

        public bool AllowUnknownItems => true;
        
        internal VariableContextualParameter(GlobalVariableType type, 
            string name, IVariablePickerService picker, IItemFromListProvider itemFromListProvider)
        {
            this.type = type;
            this.name = name;
            this.picker = picker;
            this.itemFromListProvider = itemFromListProvider;
        }

        public async Task<(long, bool)> PickValue(long value, object context)
        {
            var script = GetScript(context as SmartBaseElement);
            if (script != null)
            {
                var result = await picker.PickVariable(type, script, value);
                return (result ?? 0, result.HasValue);
            }

            return await FallbackPicker(value);
        }

        private async Task<(long, bool)> FallbackPicker(long value)
        {
            var result = await itemFromListProvider.GetItemFromList(null, false, value, "Pick a " + name);
            return (result ?? 0, result.HasValue);
        }

        public string? Prefix => null;

        private static SmartScriptBase? GetScript(SmartBaseElement? element)
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