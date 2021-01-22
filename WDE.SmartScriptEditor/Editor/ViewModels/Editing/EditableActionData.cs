using System;

namespace WDE.SmartScriptEditor.Editor.ViewModels.Editing
{
    public readonly struct EditableActionData
    {
        public EditableActionData(string name, string @group, Action command, IObservable<string> buttonName, IObservable<bool> isHidden = null)
        {
            Name = name;
            Group = @group;
            Command = command;
            ButtonName = buttonName;
            IsHidden = isHidden;
        }

        public string Name { get; }
        public string Group { get; }
        public Action Command { get; }
        public IObservable<string> ButtonName { get; }
        public IObservable<bool> IsHidden { get; }
    }
}