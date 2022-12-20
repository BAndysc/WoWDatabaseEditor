using System;
using System.Threading.Tasks;
using WDE.Parameters.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public readonly struct EditableActionData
    {
        public EditableActionData(string name, 
            string @group, 
            Func<Task> command, 
            IObservable<string> buttonName, 
            IObservable<bool>? isHidden = null,
            IParameterValueHolder<int>? value = null)
        {
            Name = name;
            Group = @group;
            Command = command;
            ButtonName = buttonName;
            IsHidden = isHidden;
            Value = value;
        }

        public string Name { get; }
        public string Group { get; }
        public Func<Task> Command { get; }
        public IObservable<string> ButtonName { get; }
        public IObservable<bool>? IsHidden { get; }
        public IParameterValueHolder<int>? Value { get; }
    }
}