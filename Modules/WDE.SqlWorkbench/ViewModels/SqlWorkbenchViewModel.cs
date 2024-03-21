using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData.Binding;
using PropertyChanged.SourceGenerator;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Services.LanguageServer;
using WDE.SqlWorkbench.Services.QuerySplitter;
using WDE.SqlWorkbench.Services.SyntaxValidator;
using AvaloniaEdit.Document;
using AvaloniaStyles;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Prism.Commands;
using WDE.Common;
using WDE.Common.Avalonia.Controls.TextMarkers;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.MVVM.Observable;
using WDE.MVVM.Utils;
using WDE.SqlQueryGenerator;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.QueryConfirmation;
using WDE.SqlWorkbench.Services.QueryUtils;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Settings;
using WDE.SqlWorkbench.Solutions;
using WDE.SqlWorkbench.Utils;
using TextDocument = AvaloniaEdit.Document.TextDocument;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SqlWorkbenchViewModel : ObservableBase, ISolutionItemDocument, IProblemSourceDocument, IPeriodicSnapshotDocument
{
    public readonly IUserQuestionsService UserQuestions;
    public readonly IClipboardService ClipboardService;
    public readonly ISqlWorkbenchPreferences Preferences;
    private const int MaxDocumentLengthForCompletions = 500_000;
    private const int MaxDocumentLengthOpenWarning = 10_000_000;
    private const int MaxDocumentLengthOpenError = 150_000_000;
    public enum LanguageServerStateEnum
    {
        NotStarted,
        Healthy,
        NotInstalled,
        Crashed,
        PausedDueToTooLongDocument,
        Disabled
    }
    
    private readonly IActionsOutputService actionsOutputService;
    [Notify] [AlsoNotify(nameof(CursorStatus))] private int caretOffset;
    [Notify] [AlsoNotify(nameof(CursorStatus))] private int caretLine = 1;
    [Notify] [AlsoNotify(nameof(CursorStatus))] private int caretColumn = 1;

    [Notify] private bool isViewBound;

    private readonly ISqlLanguageServer languageServer;
    private readonly IQueryUtility queryUtility;
    private readonly IConnectionsManager connectionsManager;
    private readonly IQuerySplitter querySplitter = new ThreadedQuerySplitter();
    private readonly ISyntaxValidator syntaxValidator = new AntlrSyntaxValidator();
    private readonly TextMarkerService textMarkerService;
    private ISqlLanguageServerFile? languageServerInstance;
    private IDisposable? languageServerDisposable;
    private IDisposable? previousTimer;
    private CancellationTokenSource? currentProcessing;
    
    public ObservableCollectionExtended<EditorCompletionData> Completions { get; } = new();
    public TextDocument Document { get; }
    public TextMarkerService TextMarkerService => textMarkerService;
    
    public IAsyncCommand ExecuteAllCommand { get; }
    public IAsyncCommand ExecuteSelectedCommand { get; }
    public IAsyncCommand StopQueryExecutionCommand { get; }
    public IAsyncCommand StopAllTasksInConnectionCommand { get; }
    public IAsyncCommand BeautifyCommand { get; }
    public IAsyncCommand<SelectResultsViewModel> CloseTabCommand { get; }
    public IAsyncCommand CloseAllTabsCommand { get; }
    public IAsyncCommand<SelectResultsViewModel> CloseOtherTabsCommand { get; }
    public IAsyncCommand<SelectResultsViewModel> CloseLeftTabsCommand { get; }
    public IAsyncCommand<SelectResultsViewModel> CloseRightTabsCommand { get; }
    public IAsyncCommand CommitCommand { get; }
    public IAsyncCommand RollbackCommand { get; }
    public IAsyncCommand ToggleIsAutoCommit { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ToggleActionsOutputViewCommand { get; }
    public IAsyncCommand RestartLanguageServerCommand { get; }
    public IAsyncCommand OpenFileCommand { get; }
    public IAsyncCommand SaveFileCommand { get; }
    public IAsyncCommand<IConnection> ChangeConnectionCommand { get; }

    [Notify] private bool isConnecting = true;
    [Notify] private bool showNonPrintableChars = false;
    [Notify] private bool wordWrap = false;
    [Notify] private int limit = 10_000;
    [Notify] private bool continueOnError = false;
    
    [Notify] private SelectResultsViewModel? selectedResult;

    private IConnection connection = null!;
    
    public ObservableCollection<SelectResultsViewModel> Results { get; } = new();

    public bool HasAnyResults => Results.Count > 0;
    
    public string CursorStatus => $"line {CaretLine}, column {CaretColumn}, location {CaretOffset}";

    public ImageUri ConnectedToIcon => connection.ConnectionData.Icon ?? ImageUri.Empty;
    
    public bool IsSessionOpened => mySqlSession is { IsConnected: true };
    
    public bool IsAutoCommit => connection.IsAutoCommit;
    
    public string ConnectedTo => connection.ConnectionData.ConnectionName;

    public Color? ConnectedToColor => connection.ConnectionData.Color;
    
    public List<int> Limits { get; } = new List<int>() { 100, 1_000, 5_000, 10_000, 50_000, 500_000 };

    private IDisposable? connectionDisposable;

    public IWindowManager WindowManager { get; }

    public IConnection Connection
    {
        get => connection;
        protected set
        {
            connectionDisposable?.Dispose();
            connection = value;
            RaisePropertyChanged(nameof(ConnectedToIcon));
            RaisePropertyChanged(nameof(ConnectedTo));
            RaisePropertyChanged(nameof(ConnectedToColor));
            RaisePropertyChanged(nameof(IsAutoCommit));
            RaisePropertyChanged(nameof(IsSessionOpened));
            RaisePropertyChanged(nameof(TaskRunning));
            connectionDisposable = new CompositeDisposable(
                connection.ToObservable(x => x.IsRunning)
                    .SubscribeAction(_ => RaisePropertyChanged(nameof(TaskRunning))),
                connection.ToObservable(x => x.IsAutoCommit)
                    .SubscribeAction(_ => RaisePropertyChanged(nameof(IsAutoCommit)))
            );
        }
    }
    
    [Notify] private bool isEnabled = true;

    [Notify] [AlsoNotify(nameof(LanguageServerToolTipText), nameof(LanguageServerHealthy))] private LanguageServerStateEnum languageServerState;
    
    public bool LanguageServerHealthy => languageServerState is LanguageServerStateEnum.Healthy or LanguageServerStateEnum.NotStarted;
    
    public bool TaskRunning => connection.IsRunning;

    public IReadOnlyList<IConnection> AllConnections => connectionsManager.StaticConnections;
    
    public string LanguageServerToolTipText => languageServerState switch
    {
        LanguageServerStateEnum.NotStarted => "Code completions are not started",
        LanguageServerStateEnum.Healthy => "Code completions seem to be working, but if they are not, press the button to restart the language server.",
        LanguageServerStateEnum.NotInstalled => "Code completions don't work, because SQLS is not found, if you have downloaded the editor binary, please report the error, if you are using Linux, macOS or built the editor manually, please download the SQLS and provide the path in the settings.",
        LanguageServerStateEnum.Crashed => "Code completions have crashed few times in a row and were disabled. You can try to restart the language server by pressing the button.",
        LanguageServerStateEnum.PausedDueToTooLongDocument => "Code completions are now paused, because your document is too long.",
        LanguageServerStateEnum.Disabled => "Code completions are disabled in the settings.",
        _ => throw new ArgumentOutOfRangeException()
    };
    
    [Notify] private IMySqlSession? mySqlSession = null;
    
    public SqlWorkbenchViewModel(IActionsOutputService actionsOutputService,
        ISqlLanguageServer languageServer,
        IConfigureService configureService,
        IQueryUtility queryUtility,
        IUserQuestionsService userQuestions,
        ISqlWorkbenchPreferences preferences,
        IClipboardService clipboardService,
        IMainThread mainThread,
        IWindowManager windowManager,
        IConnectionsManager connectionsManager,
        IQueryConfirmationService queryConfirmationService,
        IConnection connection,
        QueryDocumentSolutionItem solutionItem)
    {
        UserQuestions = userQuestions;
        ClipboardService = clipboardService;
        this.actionsOutputService = actionsOutputService;
        this.languageServer = languageServer;
        this.queryUtility = queryUtility;
        Preferences = preferences;
        this.connectionsManager = connectionsManager;
        WindowManager = windowManager;
        Connection = connection;
        Document = new TextDocument();
        textMarkerService = new TextMarkerService(Document);
        Document.UpdateStarted += DocumentUpdateStarted;
        Document.UpdateFinished += DocumentUpdateFinished;
        title = solutionItem.IsTemporary ? "New query" : Path.GetFileName(solutionItem.FileName);
        if (!solutionItem.IsTemporary && File.Exists(solutionItem.FileName))
            Document.Text = File.ReadAllText(solutionItem.FileName);
        DocumentSolutionItem = solutionItem;
        QueryConfirmationService = queryConfirmationService;
        Document.UndoStack.ToObservable(x => x.IsOriginalFile)
            .SubscribeAction(@is =>
            {
                RaisePropertyChanged(nameof(IsModified));                
                RaisePropertyChanged(nameof(IsDocumentModified));
            });
        Document.UndoStack.MarkAsOriginalFile();

        Results.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(HasAnyResults));

        AutoDispose(new ActionDisposable(() => connectionDisposable?.Dispose()));
        
        AutoDispose(new ActionDisposable(() => MySqlSession?.DisposeAsync().AsTask().ListenErrors()));

        AutoDispose(new ActionDisposable(() => languageServerDisposable?.Dispose()));
        
        AutoDispose(Results.ToStream(true)
            .SubscribeAction(x =>
            {
                if (x.Type == CollectionEventType.Add)
                    x.Item.PropertyChanged += OnResultPropertyChanged;
                else if (x.Type == CollectionEventType.Remove)
                    x.Item.PropertyChanged -= OnResultPropertyChanged;
            }));
        
        AutoDispose(mainThread.StartTimer(() =>
        {
            RaisePropertyChanged(nameof(IsSessionOpened));
            return true;
        }, TimeSpan.FromSeconds(10)));
        
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CaretOffset))
            {
                UpdateQueryMarker();
            }
        };

        OpenSettingsCommand = new DelegateCommand(() =>
        {
            var configViewModel = configureService.ShowSettings<SqlWorkbenchConfigurationViewModel>();
            if (configViewModel == null)
                return;
            
            configViewModel.SelectedConnection = configViewModel.Connections.FirstOrDefault(x => x.Id == connection.ConnectionData.Id);
        });

        CloseTabCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            var indexOf = Results.IndexOf(r);
            if (indexOf >= 0)
            {
                var wasSelected = r == SelectedResult;
                if (!await AskIfApplyAsync(r))
                    throw new TaskCanceledException();
                
                Results.Remove(r);
                if (wasSelected)
                    SelectedResult = Results.Count > indexOf ? Results[indexOf] : Results.LastOrDefault();
            }
        }).WrapMessageBox<Exception, SelectResultsViewModel>(userQuestions);

        var closeAllTabs = async () =>
        {
            for (int i = Results.Count - 1; i >= 0; --i)
            {
                if (!await AskIfApplyAsync(Results[i]))
                    throw new TaskCanceledException();

                Results.RemoveAt(i);
            }
            SelectedResult = null;
        };
        
        CloseAllTabsCommand = new AsyncAutoCommand(closeAllTabs).WrapMessageBox<Exception>(userQuestions);
        
        CloseOtherTabsCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            for (int i = Results.Count - 1; i >= 0; --i)
            {
                if (Results[i] == r)
                    continue;
                
                if (!await AskIfApplyAsync(Results[i]))
                    throw new TaskCanceledException();

                Results.RemoveAt(i);
            }
            SelectedResult = r;
        }).WrapMessageBox<Exception, SelectResultsViewModel>(userQuestions);
        
        CloseLeftTabsCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            var index = Results.IndexOf(r);
            var selectedIndex = SelectedResult == null ? -1 : Results.IndexOf(SelectedResult);
            if (index >= 0)
            {
                for (int i = index - 1; i >= 0; --i)
                {
                    if (!await AskIfApplyAsync(Results[i]))
                        throw new TaskCanceledException();
                 
                    Results.RemoveAt(i);
                }
                if (selectedIndex < index)
                    SelectedResult = Results.Count > 0 ? Results[0] : null;
            }
        }).WrapMessageBox<Exception, SelectResultsViewModel>(userQuestions);
        
        CloseRightTabsCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            var index = Results.IndexOf(r);
            var selectedIndex = SelectedResult == null ? -1 : Results.IndexOf(SelectedResult);
            if (index >= 0)
            {
                while (Results.Count > index + 1)
                {
                    if (!await AskIfApplyAsync(Results[index + 1]))
                        throw new TaskCanceledException();
                    
                    Results.RemoveAt(index + 1);
                }
                if (selectedIndex > index)
                    SelectedResult = index < Results.Count ? Results[index] : null;
            }
        }).WrapMessageBox<Exception, SelectResultsViewModel>(userQuestions);
        
        SaveFileCommand = new AsyncAutoCommand(async () =>
        {
            await closeAllTabs();
            
            string? file = DocumentSolutionItem.FileName;
            if (DocumentSolutionItem.IsTemporary)
            {
                file = await windowManager.ShowSaveFileDialog("SQL file|sql|Text file|txt|All files|*");
                if (file == null)
                    return;

                DocumentSolutionItem = new QueryDocumentSolutionItem(file, connection.ConnectionData.Id, false);
                Title = Path.GetFileName(file);
            }

            try
            {
                await File.WriteAllTextAsync(file, Document.Text);
                Document.UndoStack.MarkAsOriginalFile();
            }
            catch (Exception e)
            {
                await userQuestions.SaveErrorAsync(e);
                throw;
            }
        });
        Save = SaveFileCommand;

        OpenFileCommand = new AsyncAutoCommand(async () =>
        {
            if (IsDocumentModified)
            {
                var save = await userQuestions.AskSaveFileAsync();
                if (save == SaveDialogResult.Cancel)
                    return;
                
                if (save == SaveDialogResult.Save)
                    await SaveFileCommand.ExecuteAsync();
            }
            
            var file = await windowManager.ShowOpenFileDialog("SQL file|sql|Text file|txt|All files|*");
            if (file == null || !File.Exists(file))
                return;
            
            var fileSize = new FileInfo(file).Length;
            
            if (fileSize > MaxDocumentLengthOpenError)
            {
                await userQuestions.FileTooBigErrorAsync(fileSize, MaxDocumentLengthOpenError);
                return;
            }
            
            if (fileSize > MaxDocumentLengthOpenWarning)
            {
                if (!await userQuestions.FileTooBigWarningAsync(fileSize, MaxDocumentLengthOpenWarning))
                    return;
            }
            
            DocumentSolutionItem = new QueryDocumentSolutionItem(file, connection.ConnectionData.Id, false);
            Title = Path.GetFileName(file);
            Document.Text = await File.ReadAllTextAsync(file);
            Document.UndoStack.MarkAsOriginalFile();
        });

        ToggleActionsOutputViewCommand = new DelegateCommand(() =>
        {
            actionsOutputService.Visibility = !actionsOutputService.Visibility;
        });
        
        RestartLanguageServerCommand = new AsyncAutoCommand(RestartLanguageServerAsync);
        
        Task Execute(string query)
        {
            var action = actionsOutputService.Create(query);
            return mySqlSession!.ScheduleAsync((token, executor) => ExecuteSingleAsync(executor, query, token, action, false));
        }

        CommitCommand = new AsyncAutoCommand(async () =>
        {
            await Execute("COMMIT");
        }, () => !TaskRunning && mySqlSession != null);
        RollbackCommand = new AsyncAutoCommand(async () =>
        {
            await Execute("ROLLBACK");
        }, () => !TaskRunning && mySqlSession != null);
        ToggleIsAutoCommit = new AsyncAutoCommand(async () =>
        {
            await this.connection.SetAutoCommitAsync(!IsAutoCommit);
        }, () => !TaskRunning);
        ExecuteAllCommand = new AsyncAutoCommand(ExecuteAllAsync, () => !TaskRunning && mySqlSession != null);
        ExecuteSelectedCommand = new AsyncAutoCommand(ExecuteSelectedAsync, () => !TaskRunning && mySqlSession != null);
        BeautifyCommand = new AsyncAutoCommand(async () =>
        {
            if (languageServerInstance == null)
                return;
            
            try
            {
                IsEnabled = false;
                await mySqlSession!.ScheduleAsync(async (token, _) =>
                {
                    var task = languageServerInstance.FormatAsync(token);
                    var edits = await task;
                    foreach (var edit in edits)
                    {
                        var start = Document.GetOffset(edit.Range.Start.Line, edit.Range.Start.Character);
                        var end = Document.GetOffset(edit.Range.End.Line, edit.Range.End.Character);
                        Document.Replace(start, end - start, edit.NewText);
                    }
                });
            }
            finally
            {
                IsEnabled = true;
            }
        }, () => !TaskRunning && mySqlSession != null);
        
        StopQueryExecutionCommand = new AsyncAutoCommand(async () =>
        {
            if (mySqlSession!.AnyTaskInSession())
                await mySqlSession.CancelAllAsync();
            else
            {
                if (await userQuestions.CancelAllTasksInConnectionsAsync())
                    await connection.CancelAllAsync();
            }
        }, () => TaskRunning && mySqlSession != null);
        
        StopAllTasksInConnectionCommand = new AsyncAutoCommand(async () =>
        {
            await connection.CancelAllAsync();
        }, () => TaskRunning);

        ChangeConnectionCommand = new AsyncAutoCommand<IConnection>(async conn =>
        {
            try
            {
                Connection = conn;
                await ConnectAsync();
                await ConnectLanguageServerAsync();
                DocumentSolutionItem = DocumentSolutionItem.WithConnectionId(conn.ConnectionData.Id);
            }
            catch (Exception e)
            {
                await UserQuestions.ConnectionsErrorAsync(e);
            }
        });

        void RaiseCommandsCanExecute()
        {
            ExecuteAllCommand!.RaiseCanExecuteChanged();
            ExecuteSelectedCommand!.RaiseCanExecuteChanged();
            BeautifyCommand!.RaiseCanExecuteChanged();
            StopQueryExecutionCommand!.RaiseCanExecuteChanged();
            StopAllTasksInConnectionCommand!.RaiseCanExecuteChanged();
            CommitCommand!.RaiseCanExecuteChanged();
            RollbackCommand!.RaiseCanExecuteChanged();
            ToggleIsAutoCommit!.RaiseCanExecuteChanged();
        }
        
        On(() => TaskRunning, _ =>
        {
            RaiseCommandsCanExecute(); 
        });
        On(() => MySqlSession, _ =>
        {
            RaiseCommandsCanExecute(); 
        });
        
        ConnectAsync().ListenErrors();
    }

    private async Task<bool> AskIfApplyAsync(SelectResultsViewModel results)
    {
        if (!results.IsModified)
            return true;

        var result = await UserQuestions.ApplyPendingChangesAsync(results.Title);

        if (result == SaveDialogResult.Save)
        {
            if (!await results.SaveAsync())
                return false;
            return true;
        }
        else if (result == SaveDialogResult.DontSave)
        {
            return true;
        }
        else
            return false;
    }

    private void OnResultPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectResultsViewModel.IsModified))
            RaisePropertyChanged(nameof(IsModified));
    }

    private Task? connectTask;
    
    private async Task ConnectAsync()
    {
        if (connectTask != null)
            await connectTask;
        
        var tcs = new TaskCompletionSource();
        connectTask = tcs.Task;
        try
        {
            if (MySqlSession != null)
                await MySqlSession.DisposeAsync();
            
            MySqlSession = await connection.OpenSessionAsync();
            if (IsDisposed)
                await MySqlSession.DisposeAsync();
            IsConnecting = false;
        }
        catch (Exception e)
        {
            await UserQuestions.ConnectionsErrorAsync(e);
        }
        finally
        {
            tcs.SetResult();
            connectTask = null;
        }
    }

    public Task ExecuteAndOverrideResultsAsync(string query, SelectResultsViewModel result)
    {
        var action = actionsOutputService.Create(query);
        return mySqlSession!.ScheduleAsync((token, executor) => ExecuteSingleAsync(executor, query, token, action, false, result));
    }

    private async Task ExecuteSelectedAsync()
    {
        if (connectTask != null)
#pragma warning disable VSTHRD003
            await connectTask;
#pragma warning restore VSTHRD003
        
        querySplitter.UpdateRangesNow(Document);
        if (!querySplitter.AreRangesValid)
            throw new Exception("Invalid ranges! Fatal error, canceling. Sorry, please report this.");

        var range = querySplitter.GetRange(caretOffset);
        if (!range.HasValue)
            return;

        var query = Document.GetText(range.Value.Start, range.Value.Length);
        var action = actionsOutputService.Create(query);

        await mySqlSession!.ScheduleAsync((token, executor) => ExecuteSingleAsync(executor, query, token, action, Preferences.CloseNonModifiedTabsOnExecute));
    }

    public async Task ExecuteAsync(IReadOnlyList<string> queries, bool continueOnError, bool throwOnError, bool rollbackOnError)
    {
        await ExecuteAsyncNowInternalAsync(queries, continueOnError, throwOnError, rollbackOnError);
    }
    
    private async Task ExecuteAsyncNowInternalAsync(IReadOnlyList<string> queries, bool continueOnError, bool throwOnError, bool rollbackOnError)
    {
        if (queries.Count == 0)
            return;

        if (connectTask != null)
#pragma warning disable VSTHRD003
            await connectTask;
#pragma warning restore VSTHRD003
        
        // we do it before schedule, so that the action is added to the UI list before the execution starts
        var action = actionsOutputService.Create(queries[0]);

        await mySqlSession!.ScheduleAsync(async (token, executor) =>
        {
            for (var index = 0; index < queries.Count; index++)
            {
                var query = queries[index];
                
                if (index > 0) // index 0 was created before the loop
                    action = actionsOutputService.Create(queries[index]);
                
                try
                {
                    await ExecuteSingleAsync(executor, query, token, action, Preferences.CloseNonModifiedTabsOnExecute && index == 0);
                }
                catch (Exception)
                {
                    if (rollbackOnError)
                    {
                        action = actionsOutputService.Create("ROLLBACK");
                        await ExecuteSingleAsync(executor, "ROLLBACK", token, action, false);
                    }
                    
                    if (throwOnError)
                        throw;
                    
                    if (!continueOnError)
                        break;
                }
            }
        });
    }

    private async Task ExecuteAllAsync()
    {
        if (connectTask != null)
#pragma warning disable VSTHRD003
            await connectTask;
#pragma warning restore VSTHRD003

        querySplitter.UpdateRangesNow(Document);
        if (!querySplitter.AreRangesValid)
            throw new Exception("Invalid ranges! Fatal error, canceling. Sorry, please report this.");

        List<string> queries = new();
        foreach (var range in querySplitter.Ranges)
            queries.Add(Document.GetText(range.Start, range.Length));

        await ExecuteAsyncNowInternalAsync(queries, this.continueOnError, false, false);
    }

    private void TryCloseNonModifiedResultTabs()
    {
        if (Preferences.CloseNonModifiedTabsOnExecute)
        {
            for (int i = Results.Count - 1; i >= 0; --i)
            {
                if (!Results[i].IsModified)
                    Results.RemoveAt(i);
            }
        }
    }

    private async Task ExecuteSingleAsync(IMySqlQueryExecutor executor, 
        string query, 
        CancellationToken cancellationToken, 
        IActionOutput action,
        bool closeExistingResultsUnlessModified,
        SelectResultsViewModel? result = null)
    {
        action.TimeStarted = DateTime.Now;
        action.Status = ActionStatus.Started;
        
        bool CheckIfCancelled()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                action.Response = "Canceled";
                action.Status = ActionStatus.Canceled;
                return true;
            }
            return false;
        }
        try
        {
            var res = await executor.ExecuteSqlAsync(query, limit, cancellationToken);
            action.TimeFinished = DateTime.Now;

            if (CheckIfCancelled())
                return;

            if (!res.IsNonQuery)
            {
                var isSelect = queryUtility.IsSimpleSelect(query, out var select);
                if (result == null)
                {
                    var type = await executor.GetTableTypeAsync(select.From.Schema, select.From.Table, cancellationToken);
                    if (CheckIfCancelled())
                        return;

                    if (isSelect && type == TableType.Table)
                    {
                        var schema = select.From.Schema;
                        if (schema == null)
                            schema = await executor.GetCurrentDatabaseAsync(cancellationToken);
                        if (schema == null)
                            throw new Exception("Weird error. Just invoked a SELECT, but current database is null, but the query didn't specify the database? Pls report that.");
                        var tableInfo = await executor.GetTableColumnsAsync(schema, select.From.Table, cancellationToken);
                        
                        if (CheckIfCancelled())
                            return;
                        
                        if (closeExistingResultsUnlessModified)
                            TryCloseNonModifiedResultTabs();
                        Results.Add(new SelectSingleTableViewModel(this, action, in res, select, tableInfo));
                    }
                    else
                    {
                        if (closeExistingResultsUnlessModified)
                            TryCloseNonModifiedResultTabs();
                        Results.Add(new SelectResultsViewModel(this, action, in res));
                    }
                    SelectedResult = Results[^1];   
                }
                else
                {
                    IReadOnlyList<ColumnInfo>? columns = null;
                    if (isSelect && result is SelectSingleTableViewModel singleTable)
                    {
                        var schema = select.From.Schema;
                        if (schema == null)
                            schema = await executor.GetCurrentDatabaseAsync(cancellationToken);
                        if (schema == null)
                            throw new Exception("Weird error. Just invoked a SELECT, but current database is null, but the query didn't specify the database? Pls report that.");
                        columns = await executor.GetTableColumnsAsync(schema, select.From.Table, cancellationToken);
                    }
                    result.UpdateResults(in res, columns);
                }
                action.Response = $"{res.AffectedRows} row(s) returned";
            }
            else
            {
                Debug.Assert(result == null);

                if (queryUtility.IsUseDatabase(query, out var useDatabase) &&
                    languageServerInstance != null)
                {
                    await languageServerInstance.ChangeDatabaseAsync(useDatabase);
                }
                
                if (res.AffectedRows == 1)
                    action.Response = $"{res.AffectedRows} row affected";
                else
                    action.Response = $"{res.AffectedRows} rows affected";
            }

            action.Status = ActionStatus.Success;
        }
        catch (Exception e)
        {
            action.TimeFinished = DateTime.Now;
            action.Response = "Error: " + e.Message;
            action.Exception = e;
            action.Status = ActionStatus.Error;
            throw;
        }
    }

    private async Task RestartLanguageServerAsync()
    {
        if (languageServerInstance != null)
            await languageServerInstance.RestartLanguageServerAsync();
        else
            await ConnectLanguageServerAsync();
    }

    private bool languageServerStarting;
    
    private async Task ConnectLanguageServerAsync()
    {
        if (!Preferences.UseCodeCompletion)
        {
            LanguageServerState = LanguageServerStateEnum.Disabled;
            return;
        }
        
        if (languageServerStarting)
            return;

        try
        {
            languageServerStarting = true;
            var connId = await languageServer.ConnectAsync(connection.ConnectionData.Id, connection.ConnectionData.Credentials);
            
            if (IsDisposed)
                return;

            if (languageServerDisposable != null)
            {
                languageServerDisposable.Dispose();
                languageServerDisposable = null;
            }
            
            languageServerInstance = await languageServer.NewFileAsync(connId);

            if (IsDisposed)
            {
                languageServerInstance.Dispose();
                return;
            }

            languageServerDisposable = new CompositeDisposable(languageServerInstance.ServerAlive.SubscribeAction(
                isAlive =>
                {
                    if (!isAlive)
                    {
                        LanguageServerState = LanguageServerStateEnum.Crashed;
                    }
                    else
                    {
                        if (LanguageServerState is LanguageServerStateEnum.Crashed
                            or LanguageServerStateEnum.NotStarted)
                            LanguageServerState = LanguageServerStateEnum.Healthy;
                    }
                }), languageServerInstance);
        }
        catch (Exception)
        {
            LanguageServerState = LanguageServerStateEnum.NotInstalled;
            throw;
        }
        finally
        {
            languageServerStarting = false;
        }
    }

    private void DocumentUpdateFinished(object? sender, EventArgs e)
    {
        previousTimer?.Dispose();
        previousTimer = null;
        var textLength = Document.TextLength;
        char prevChar = textLength > caretOffset && caretOffset >= 0 ? Document.GetCharAt(caretOffset) : '\0';
        char prevPrevChar = textLength > caretOffset && caretOffset >= 1 ? Document.GetCharAt(caretOffset - 1) : '\0';

        void Process()
        {
            var snapshot = Document.CreateSnapshot();
            currentProcessing?.Cancel();
            currentProcessing = new CancellationTokenSource();
            ProcessDataAsync(snapshot, currentProcessing.Token).ListenErrors();
        }
        
        // if we are typing the first letter in a word, we don't want to add any delay
        if (char.IsLetter(prevChar) && (prevPrevChar == '\0' || char.IsWhiteSpace(prevPrevChar)))
            Process();
        else
            previousTimer = DispatcherTimer.RunOnce(Process, TimeSpan.FromMilliseconds(250));
    }
    
    private void DocumentUpdateStarted(object? sender, EventArgs e)
    {
        querySplitter.InvalidateRanges();
        UpdateQueryMarker();
        if (IsViewBound && languageServerInstance == null)
            ConnectLanguageServerAsync().ListenErrors();
    }

    private void UpdateQueryMarker()
    {
        textMarkerService.RemoveAll(TextMarkerTypes.CircleInScrollBar);
        if (!querySplitter.AreRangesValid)
            return;

        var range = querySplitter.GetRange(caretOffset);
        if (!range.HasValue) 
            return;
        
        var marker = textMarkerService.Create(TextMarkerTypes.CircleInScrollBar, range.Value.Start, range.Value.Length);
        var color = SystemTheme.EffectiveThemeIsDark ? new Color(22, 255, 255, 255) : new Color(22, 0, 0, 0);
        marker.BackgroundBrush = new SolidColorBrush(color);
    }
    
    private async Task ProcessDataAsync(ITextSource snapshot, CancellationToken token)
    {
        if (!await querySplitter.UpdateRangesAsync(snapshot))
            return;
        
        UpdateQueryMarker();
        
        Debug.Assert(querySplitter.AreRangesValid);

        if (languageServerInstance == null)
            return;

        if (Document.TextLength > MaxDocumentLengthForCompletions)
        {
            LanguageServerState = LanguageServerStateEnum.PausedDueToTooLongDocument;
            return;
        }
        else if (LanguageServerState == LanguageServerStateEnum.PausedDueToTooLongDocument)
            LanguageServerState = LanguageServerStateEnum.Healthy;
        
        languageServerInstance.UpdateText(Document.Text);
        await ComputeCompletionAsync(false);

        var tasks = querySplitter.Ranges
            .Select(async r =>
            {
                var output = new List<SyntaxError>();
                await syntaxValidator.ValidateAsync(snapshot, r.Start, r.Length, output, default);
                return output.Select(x => new SyntaxError(x.Line + r.Line, x.Start + r.Start, x.Length, x.Message)).ToList();
            })
            .ToArray();
        var lists = await Task.WhenAll(tasks);

        if (token.IsCancellationRequested)
            return;
        
        problemsList.Swap();
        var inspections = problemsList.Value;
        inspections.Clear();
        textMarkerService.RemoveAll(TextMarkerTypes.SquigglyUnderline);
        foreach (var list in lists)
        {
            foreach (var err in list)
            {
                var marker = textMarkerService.Create(TextMarkerTypes.SquigglyUnderline, err.Start, err.Length);
                marker.MarkerColor = Colors.Red;
                marker.ToolTip = err.Message;
                inspections.Add(new SqlInspectionResult(err.Message, err.Line));
            }
        }
        problems.Value = inspections;
    }
    
    public async Task ComputeCompletionAsync(bool force)
    {
        if (languageServerInstance == null)
            return;
        
        try
        {
            if (!force)
            {
                if (CaretOffset - 1 >= 0 && Document.GetCharAt(CaretOffset - 1) is var ch && (ch == ';' || char.IsWhiteSpace(ch) || ch == ','))
                {
                    Completions.Clear();
                    return;
                }

                if (CaretOffset < Document.TextLength && Document.GetCharAt(CaretOffset) is not ' ' and not '\n' and not '\r' and not '\t')
                {
                    Completions.Clear();
                    return;
                }
            }
            
            var completions = await languageServerInstance.GetCompletionsAsync(CaretLine, CaretColumn);

            var lastWord = GetLastWord(CaretOffset - 1);

            using var _ = Completions.SuspendNotifications();
            Completions.Clear();
            foreach (var c in completions)
            {
                if (c.Label.Equals(lastWord, StringComparison.OrdinalIgnoreCase))
                    continue;
                Completions.Add(new EditorCompletionData(c.Label, c.Detail, GetIcon(c.Kind, c.Detail)));
            }
        }
        catch (Exception e)
        {
            LOG.LogWarning(e, "Error while computing completions");
            throw;
        }
    }

    public ImageUri GetIcon(CompletionItemKind kind, string? detail)
    {
        if (kind == CompletionItemKind.Keyword)
            return new ImageUri("Icons/icon_mini_keyword.png");
        if (kind == CompletionItemKind.Function)
            return new ImageUri("Icons/icon_mini_func.png");
        if (kind == CompletionItemKind.Field)
            return detail?.StartsWith("subquery") ?? false ? new ImageUri("Icons/icon_mini_subquery.png") : new ImageUri("Icons/icon_mini_column.png");
        if (kind == CompletionItemKind.Snippet)
            return new ImageUri("Icons/icon_mini_join.png");
        if (kind == CompletionItemKind.Class)
            return new ImageUri("Icons/icon_mini_table.png");
        if (kind == CompletionItemKind.Module)
            return new ImageUri("Icons/icon_mini_schema.png");
        return new ImageUri("Icons/icon_mini_view.png");
    }
    
    private string GetLastWord(int offset)
    {
        StringBuilder sb = new();
        int length = 0;
        while (offset >= 0 && length < 100) // prevent long loops by limiting the size
        {
            char c = Document.GetCharAt(offset);
            if (char.IsWhiteSpace(c))
                break;
            else
                sb.Insert(0, c);
            offset--;
            length++;
        }

        return sb.ToString();
    }

    public async Task<string?> GetHoverInfoAsync(int line, int column)
    {
        if (languageServerInstance == null || LanguageServerState == LanguageServerStateEnum.PausedDueToTooLongDocument)
            return null;
        
        var hover = await languageServerInstance.GetHoverAsync(line, column);
        if (hover != null)  
        {
            if (hover.Contents.HasMarkupContent)
                return hover.Contents.MarkupContent!.Value.Replace("&nbsp;", "");

            return "todo?";
        }
        else
        {
            var help = await languageServerInstance.GetSignatureHelpAsync(line, column);
            if (help != null)
            {
                var sb = new StringBuilder();
                foreach (var s in help.Signatures)
                {
                    sb.Append(s.Label);
                    sb.Append("\n");
                    sb.Append(s.Documentation);
                    sb.Append("\n");
                }

                return sb.ToString();
            }
        }

        return null;
    }

    [Notify] private string title;
    public bool IsModified => (!DocumentSolutionItem.IsTemporary && IsDocumentModified) || IsAnyResultModified;
    private bool IsAnyResultModified => Results.Any(x => x.IsModified);
    public bool IsDocumentModified => !Document.UndoStack.IsOriginalFile;
    public ImageUri? Icon { get; } = new ImageUri("Icons/document_sql.png");
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save { get; }
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public IObservable<IReadOnlyList<IInspectionResult>> Problems => problems;
    private ReactiveProperty<IReadOnlyList<IInspectionResult>> problems = new ReactiveProperty<IReadOnlyList<IInspectionResult>>(new List<IInspectionResult>());
    private DoubleBufferedValue<List<IInspectionResult>> problemsList = new DoubleBufferedValue<List<IInspectionResult>>();
    public QueryDocumentSolutionItem DocumentSolutionItem { get; private set; }
    public IQueryConfirmationService QueryConfirmationService { get; }
    public ISolutionItem SolutionItem => DocumentSolutionItem;
    public async Task<IQuery> GenerateQuery() => Queries.Empty(DataDatabaseType.World);
    public bool ShowExportToolbarButtons => false;
    
    public string? TakeSnapshot()
    {
        if (DocumentSolutionItem.IsTemporary || IsDocumentModified)
           return Document.Text;
        
        return null;
    }

    public void RestoreSnapshot(string snapshot)
    {
        Document.Text = snapshot;
    }
}