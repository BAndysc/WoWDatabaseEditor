using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.ViewModels;

public class RowPickerViewModel : ObservableBase, IDialog, IClosableDialog
{
    private readonly ViewModelBase baseViewModel;
    private readonly ISolutionItemEditorRegistry solutionItemEditorRegistry;
    private readonly ISessionService sessionService;
    private readonly IMessageBoxService messageBoxService;
    private readonly bool noSaveMode;

    public bool DisablePicking { get; set; }

    public RowPickerViewModel(ViewModelBase baseViewModel,
        ITaskRunner taskRunner, 
        ISolutionItemSqlGeneratorRegistry queryGeneratorRegistry, 
        IClipboardService clipboardService,
        IWindowManager windowManager,
        IEventAggregator eventAggregator,
        ISolutionItemEditorRegistry solutionItemEditorRegistry,
        ISessionService sessionService,
        IMessageBoxService messageBoxService,
        ISolutionTasksService solutionTasksService,
        bool noSaveMode = false)
    {
        this.baseViewModel = baseViewModel;
        this.solutionItemEditorRegistry = solutionItemEditorRegistry;
        this.sessionService = sessionService;
        this.messageBoxService = messageBoxService;
        this.noSaveMode = noSaveMode;
        Watch(baseViewModel, o => o.IsModified, nameof(Title));
        ExecuteChangedCommand = noSaveMode ? AlwaysDisabledCommand.Command : new AsyncAutoCommand(async () =>
        {
            baseViewModel.Save.Execute(null);
            eventAggregator.GetEvent<DatabaseTableChanged>().Publish(baseViewModel.TableDefinition.TableName);
            await taskRunner.ScheduleTask("Update session", async () => await sessionService.UpdateQuery(baseViewModel));
            if (solutionTasksService.CanReloadRemotely)
                await solutionTasksService.ReloadSolutionRemotelyTask(baseViewModel.SolutionItem);
        });
        CopyCurrentSqlCommand = new AsyncAutoCommand(async () =>
        {
            await taskRunner.ScheduleTask("Generating SQL",
                async () => { clipboardService.SetText((await baseViewModel.GenerateQuery()).QueryString);});
        });
        GenerateCurrentSqlCommand = new AsyncAutoCommand(async () =>
        {
            var sql = await baseViewModel.GenerateQuery();
            var item = new MetaSolutionSQL(new JustQuerySolutionItem(sql.QueryString));
            var editor = solutionItemEditorRegistry.GetEditor(item);
            await windowManager.ShowDialog((IDialog)editor);
        });
        PickSelected = new AsyncAutoCommand(async () =>
        {
            await AskIfSave(false);
        });
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        Accept = PickSelected;
    }
    
    public void Pick(DatabaseEntity pickedEntity)
    {
        overrideSelectedRow = pickedEntity;
        PickSelected.Execute(null);
    }
    
    private async Task AskIfSave(bool cancel)
    {
        if (baseViewModel.IsModified)
        {
            var result = await messageBoxService.ShowDialog(new MessageBoxFactory<int>()
                .SetTitle("Save changes")
                .SetMainInstruction($"{Title} has unsaved changes")
                .SetContent("Do you want to save them before picking the row?")
                .WithNoButton(1)
                .WithYesButton(2)
                .WithCancelButton(0)
                .Build());
            if (result == 0)
                return;
            if (result == 2)
                ExecuteChangedCommand.Execute(null);
        }
        if (cancel)
            CloseCancel?.Invoke();
        else
            CloseOk?.Invoke();
    }

    public void OnClose()
    {
        if (baseViewModel.IsModified)
        {
            AskIfSave(true).ListenErrors();
        }
        else
            CloseCancel?.Invoke();
    }

    private DatabaseEntity? overrideSelectedRow;
    public DatabaseEntity? SelectedRow => overrideSelectedRow ?? baseViewModel.FocusedEntity;
    public ViewModelBase? MainViewModel => baseViewModel;

    public int DesiredWidth => 900;
    public int DesiredHeight => 600;
    public string Title
    {
        get
        {
            if (baseViewModel.IsModified)
                return baseViewModel.Title + " (*)";
            return baseViewModel.Title;
        }
    }
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }

    public ICommand ExecuteChangedCommand { get; }
    public ICommand GenerateCurrentSqlCommand { get; }
    public ICommand CopyCurrentSqlCommand { get; }
    public ICommand PickSelected { get; }

    public event Action? CloseCancel;
    public event Action? CloseOk;
}

public class JustQuerySolutionItem : ISolutionItem
{
    public string Query { get; }
    public bool IsContainer => false;
    public ObservableCollection<ISolutionItem>? Items => null;
    public string? ExtraId => null;
    public bool IsExportable => true;
    public ISolutionItem Clone() => throw new NotImplementedException();

    public JustQuerySolutionItem(string query)
    {
        Query = query;
    }
    
    public JustQuerySolutionItem(IQuery query)
    {
        Query = query.QueryString;
    }
}

[AutoRegister]
[SingleInstance]
public class JustQuerySolutionItemGenerator : ISolutionItemSqlProvider<JustQuerySolutionItem>
{
    public Task<IQuery> GenerateSql(JustQuerySolutionItem item)
    {
        return Task.FromResult(Queries.Raw(item.Query));
    }
}