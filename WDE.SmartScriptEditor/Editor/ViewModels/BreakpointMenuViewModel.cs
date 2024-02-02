using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SmartScriptEditor.Debugging;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels;

public class BreakpointMenuViewModel : ObservableBase
{
    public ObservableCollection<BreakpointMenuItemViewModel> MenuItems { get; } = new();
}

public class BreakpointMenuItemViewModel
{
    public BreakpointMenuItemViewModel(SmartBaseElement element, ICommand command)
    {
        Command = command;
        if (element is SmartEvent e)
        {
            var eventReadable = e.Readable.RemoveTags();
            Type = SmartBreakpointType.Event;
            Header = eventReadable;
            CommandParameter = element;
        }
        else if (element is SmartAction a)
        {
            var sourceReadable = a.Source.Readable.RemoveTags();
            var fullActionReadable = a.Readable.RemoveTags();
            var actionReadable = fullActionReadable.Replace(sourceReadable + ": ", "");
            Type = SmartBreakpointType.Action;
            Header = actionReadable;
            CommandParameter = element;
        }
        else if (element is SmartTarget t)
        {
            var targetReadable = t.Readable.RemoveTags();
            Type = SmartBreakpointType.Target;
            Header = targetReadable;
            CommandParameter = element;
        }
        else if (element is SmartSource s)
        {
            var sourceReadable = s.Readable.RemoveTags();
            Type = SmartBreakpointType.Source;
            Header = sourceReadable;
            CommandParameter = element;
        }
        else
            throw new ArgumentOutOfRangeException(nameof(element));
    }

    public BreakpointMenuItemViewModel(SmartBreakpointType type, string header, ICommand command, object? commandParameter)
    {
        Type = type;
        Header = header;
        Command = command;
        CommandParameter = commandParameter;
    }

    public SmartBreakpointType Type { get; }
    public string Header { get; }
    public ICommand Command { get; }
    public object? CommandParameter { get; }
}