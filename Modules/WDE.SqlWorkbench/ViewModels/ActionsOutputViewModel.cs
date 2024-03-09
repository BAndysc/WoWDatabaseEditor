using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input.Platform;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Collections;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.MVVM;
using WDE.SqlWorkbench.Services.ActionsOutput;

namespace WDE.SqlWorkbench.ViewModels;

public partial class ActionsOutputViewModel : ObservableBase, ITool, IActionsOutputService
{
    private readonly IMainThread mainThread;
    [Notify] private bool isSelected;
    [Notify] private bool visibility;
    [Notify] [AlsoNotify(nameof(FocusedItem))] private int focusedIndex = -1;
    private ObservableCollection<ActionOutputViewModel> actions = new();
    private int actionIndex = 1;

    public string Title => "Queries output";
    public string UniqueId => "sql_actions_output";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Bottom;
    public bool OpenOnStart => false;

    public IMultiIndexContainer Selection { get; } = new MultiIndexContainer();
    public ActionOutputViewModel? FocusedItem => focusedIndex >= 0 && focusedIndex < actions.Count ? actions[focusedIndex] : null;
    
    public IIndexedCollection<ActionOutputViewModel> Actions { get; }
    
    public ICommand ClearConsoleCommand { get; }
    public ICommand CopySelectedQueriesCommand { get; }
    public ICommand CopySelectedResponsesCommand { get; }
    public ICommand CopySelectedDurationsCommand { get; }
    
    public ActionsOutputViewModel(IMainThread mainThread)
    {
        this.mainThread = mainThread;
        Actions = actions.AsIndexedCollection();
        ClearConsoleCommand = new DelegateCommand(() => 
        {
            Selection.Clear();
            actions.Clear();
            FocusedIndex = -1;
        });
        
        CopySelectedQueriesCommand = new AsyncAutoCommand(async () =>
        {
            var text = string.Join(";\n", SelectedViewModels.Select(x => x.OriginalQuery));
            await Application.Current!.GetTopLevel()!.Clipboard!.SetTextAsync(text);
        });
        
        CopySelectedResponsesCommand = new AsyncAutoCommand(async () => 
        {
            var text = string.Join("\n", SelectedViewModels.Select(x => x.Response));
            await Application.Current!.GetTopLevel()!.Clipboard!.SetTextAsync(text);
        });
        
        CopySelectedDurationsCommand = new AsyncAutoCommand(async () => 
        {
            var text = string.Join("\n", SelectedViewModels.Select(x => x.DurationAsString));
            await Application.Current!.GetTopLevel()!.Clipboard!.SetTextAsync(text);
        });
    }

    private IEnumerable<ActionOutputViewModel> SelectedViewModels
    {
        get
        {
            return Selection.All().Where(index => index >= 0 && index < actions.Count).Select(index => actions[index]);
        }
    }

    public IActionOutput Create(string query)
    {
        var action = new ActionOutputViewModel(mainThread, actionIndex++, query);
        actions.Add(action);
        FocusedIndex = actions.Count - 1;
        return action;
    }
}