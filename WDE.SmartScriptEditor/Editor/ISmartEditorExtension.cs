using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WDE.Common.Types;
using WDE.MVVM;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor;

public interface ISmartEditorExtension
{
    Task BeforeLoad(SmartScriptEditorViewModel editorViewModel, ISmartScriptSolutionItem item) => Task.CompletedTask;
    IReadOnlyList<SmartExtensionCommand>? Commands => null;
    ISmartHeaderViewModel? CreateHeader(SmartScriptEditorViewModel editorViewModel, ISmartScriptSolutionItem item) => null;
}

public class SmartExtensionNotification : ObservableBase
{
    public SmartExtensionNotification(string header, string content, string actionName, ICommand action)
    {
        Header = header;
        Content = content;
        Action = action;
        ActionName = actionName;
    }

    private bool isOpened = true;

    public bool IsOpened
    {
        get => isOpened;
        set
        {
            isOpened = value;
            RaisePropertyChanged();
        }
    }

    public string Header { get; }
    public string Content { get; }
    public string ActionName { get; }
    public ICommand Action { get; private set; }

    public SmartExtensionNotification WrapCommand(Func<ICommand, ICommand> wrapper)
    {
        Action = wrapper(Action);
        return this;
    }
}

public class SmartExtensionCommand
{
    public SmartExtensionCommand(string name, ImageUri icon, ICommand command)
    {
        Name = name;
        Command = command;
        Icon = icon;
    }

    public string Name { get; }
    public ICommand Command { get; }
    public ImageUri Icon { get; }
}