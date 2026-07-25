using System;

namespace WDE.DbScriptsEditor.Editor.ViewModels.Editing
{
    // Copy-adapted from WDE.EventAiEditor: a "button row" in the edit dialog (change command,
    // pick source/target) whose label live-updates via the observable.
    public readonly struct EditableActionData
    {
        public EditableActionData(string name, string @group, Action command, IObservable<string> buttonName, IObservable<bool>? isHidden = null)
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
        public IObservable<bool>? IsHidden { get; }
    }
}
