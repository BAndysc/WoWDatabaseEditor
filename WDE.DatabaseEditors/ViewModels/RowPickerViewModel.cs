using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.ViewModels;

public class RowPickerViewModel : ObservableBase, IDialog, IWindowViewModel, IClosableDialog
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
        Watch(baseViewModel, o => o.Title, nameof(Title));
        ExecuteChangedCommand = noSaveMode ? AlwaysDisabledAsyncCommand.Command : new AsyncAutoCommand(async () =>
        {
            await baseViewModel.Save.ExecuteAsync();
            eventAggregator.GetEvent<DatabaseTableChanged>().Publish(baseViewModel.TableDefinition.Id);
            if (sessionService.IsOpened)
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
            var item = new MetaSolutionSQL(new JustQuerySolutionItem(sql));
            using var editor = solutionItemEditorRegistry.GetEditor(item);
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
        AutoDispose(new ActionDisposable(baseViewModel.Dispose));
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
                await ExecuteChangedCommand.ExecuteAsync();
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

    public event Action? Close;

    private DatabaseEntity? overrideSelectedRow;
    public DatabaseEntity? SelectedRow => overrideSelectedRow ?? baseViewModel.FocusedEntity;
    public ViewModelBase? MainViewModel => baseViewModel;

    public int DesiredWidth => 900;
    public int DesiredHeight => 600;
    public ImageUri? Icon => baseViewModel.Icon;
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

    public IAsyncCommand ExecuteChangedCommand { get; }
    public ICommand GenerateCurrentSqlCommand { get; }
    public ICommand CopyCurrentSqlCommand { get; }
    public ICommand PickSelected { get; }

    public event Action? CloseCancel;
    public event Action? CloseOk;
}

public class JustQuerySolutionItem : ISolutionItem
{
    public string Query { get; }
    public DataDatabaseType Database { get; }
    public bool IsContainer => false;
    public ObservableCollection<ISolutionItem>? Items => null;
    public string? ExtraId => null;
    public bool IsExportable => true;
    public ISolutionItem Clone() => throw new NotImplementedException();

    public JustQuerySolutionItem(IQuery query)
    {
        Query = query.QueryString;
        Database = query.Database;
    }
}

[AutoRegister]
[SingleInstance]
public class JustQuerySolutionItemGenerator : ISolutionItemSqlProvider<JustQuerySolutionItem>
{
    public Task<IQuery> GenerateSql(JustQuerySolutionItem item)
    {
        return Task.FromResult(Queries.Raw(item.Database, item.Query));
    }
}