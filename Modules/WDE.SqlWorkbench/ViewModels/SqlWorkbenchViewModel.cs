using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
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
using WDE.SqlWorkbench.Services.TextMarkers;
using AvaloniaEdit.Document;
using AvaloniaStyles;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Prism.Commands;
using WDE.Common.Disposables;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Types;
using WDE.MVVM.Observable;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.QueryUtils;
using WDE.SqlWorkbench.Settings;
using WDE.SqlWorkbench.Utils;
using TextDocument = AvaloniaEdit.Document.TextDocument;

namespace WDE.SqlWorkbench.ViewModels;

internal partial class SqlWorkbenchViewModel : ObservableBase, IProblemSourceDocument
{
    public IMessageBoxService MessageBoxService { get; }
    public IClipboardService ClipboardService { get; }
    private const int MaxDocumentLength = 500_000;
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
    private readonly ISqlWorkbenchPreferences preferences;
    private readonly IConnection connection;
    private readonly IQuerySplitter querySplitter = new ThreadedQuerySplitter();
    private readonly ISyntaxValidator syntaxValidator = new AntlrSyntaxValidator();
    private readonly TextMarkerService textMarkerService;
    private ISqlLanguageServerFile? languageServerInstance;
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

    [Notify] private bool isConnecting = true;
    [Notify] private bool showNonPrintableChars = false;
    [Notify] private bool wordWrap = false;
    [Notify] private int limit = 10_000;
    [Notify] private bool continueOnError = false;
    
    [Notify] private SelectResultsViewModel? selectedResult;
    
    public ObservableCollection<SelectResultsViewModel> Results { get; } =
        new ObservableCollection<SelectResultsViewModel>();

    public bool HasAnyResults => Results.Count > 0;
    
    public string CursorStatus => $"line {CaretLine}, column {CaretColumn}, location {CaretOffset}";

    public ImageUri ConnectedToIcon => connection.ConnectionData.Icon ?? ImageUri.Empty;
    
    public bool IsSessionOpened => mySqlSession is { IsConnected: true };
    
    public bool IsAutoCommit => connection.IsAutoCommit;
    
    public string ConnectedTo => connection.ConnectionData.ConnectionName;

    public Color? ConnectedToColor => connection.ConnectionData.Color;
    
    public List<int> Limits { get; } = new List<int>() { 100, 1_000, 5_000, 10_000, 50_000, 500_000 };
    
    [Notify] private bool isEnabled = true;

    [Notify] [AlsoNotify(nameof(LanguageServerToolTipText), nameof(LanguageServerHealthy))] private LanguageServerStateEnum languageServerState;
    
    public bool LanguageServerHealthy => languageServerState is LanguageServerStateEnum.Healthy or LanguageServerStateEnum.NotStarted;
    
    public bool TaskRunning => connection.IsRunning;
    
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
    
    private IMySqlSession mySqlSession = null!;
    
    public SqlWorkbenchViewModel(IActionsOutputService actionsOutputService,
        ISqlLanguageServer languageServer,
        IConfigureService configureService,
        IQueryUtility queryUtility,
        IMessageBoxService messageBoxService,
        ISqlWorkbenchPreferences preferences,
        IClipboardService clipboardService,
        IConnection connection)
    {
        MessageBoxService = messageBoxService;
        ClipboardService = clipboardService;
        this.actionsOutputService = actionsOutputService;
        this.languageServer = languageServer;
        this.queryUtility = queryUtility;
        this.preferences = preferences;
        this.connection = connection;
        Document = new TextDocument();
        textMarkerService = new TextMarkerService(Document);
        Document.UpdateStarted += DocumentUpdateStarted;
        Document.UpdateFinished += DocumentUpdateFinished;
        title = "New query";
        Document.UndoStack.ToObservable(x => x.IsOriginalFile)
            .SubscribeAction(@is => IsDocumentModified = !@is);

        Results.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(HasAnyResults));
        AutoDispose(connection.ToObservable(x => x.IsRunning).SubscribeAction(_ => RaisePropertyChanged(nameof(TaskRunning))));
        AutoDispose(connection.ToObservable(x => x.IsAutoCommit).SubscribeAction(_ => RaisePropertyChanged(nameof(IsAutoCommit))));
        
        AutoDispose(Results.ToStream(true)
            .SubscribeAction(x =>
            {
                if (x.Type == CollectionEventType.Add)
                    x.Item.PropertyChanged += OnResultPropertyChanged;
                else if (x.Type == CollectionEventType.Remove)
                    x.Item.PropertyChanged -= OnResultPropertyChanged;
            }));
        
        AutoDispose(DispatcherTimer.Run(() =>
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

        ToggleActionsOutputViewCommand = new DelegateCommand(() =>
        {
            actionsOutputService.Visibility = !actionsOutputService.Visibility;
        });
        
        CloseTabCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            var indexOf = Results.IndexOf(r);
            if (indexOf >= 0)
            {
                var wasSelected = r == SelectedResult;
                if (!await AskIfApplyAsync(r))
                    return;
                
                Results.Remove(r);
                if (wasSelected)
                    SelectedResult = Results.Count > indexOf ? Results[indexOf] : Results.LastOrDefault();
            }
        });
        
        CloseAllTabsCommand = new AsyncAutoCommand(async () =>
        {
            for (int i = Results.Count - 1; i >= 0; --i)
            {
                if (!await AskIfApplyAsync(Results[i]))
                    return;

                Results.RemoveAt(i);
            }
            SelectedResult = null;
        });
        
        CloseOtherTabsCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            for (int i = Results.Count - 1; i >= 0; --i)
            {
                if (Results[i] == r)
                    continue;
                
                if (!await AskIfApplyAsync(Results[i]))
                    return;

                Results.RemoveAt(i);
            }
            SelectedResult = r;
        });
        
        CloseLeftTabsCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            var index = Results.IndexOf(r);
            var selectedIndex = SelectedResult == null ? -1 : Results.IndexOf(SelectedResult);
            if (index >= 0)
            {
                for (int i = index - 1; i >= 0; --i)
                {
                    if (!await AskIfApplyAsync(Results[i]))
                        return;
                 
                    Results.RemoveAt(i);
                }
                if (selectedIndex < index)
                    SelectedResult = Results.Count > 0 ? Results[0] : null;
            }
        });
        
        CloseRightTabsCommand = new AsyncAutoCommand<SelectResultsViewModel>(async r =>
        {
            var index = Results.IndexOf(r);
            var selectedIndex = SelectedResult == null ? -1 : Results.IndexOf(SelectedResult);
            if (index >= 0)
            {
                while (Results.Count > index + 1)
                {
                    if (!await AskIfApplyAsync(Results[index + 1]))
                        return;
                    Results.RemoveAt(index + 1);
                }
                if (selectedIndex > index)
                    SelectedResult = index < Results.Count ? Results[index] : null;
            }
        });
        
        RestartLanguageServerCommand = new AsyncAutoCommand(RestartLanguageServerAsync);
        
        Task Execute(string query)
        {
            var action = actionsOutputService.Create(query);
            return mySqlSession.ScheduleAsync((token, executor) => ExecuteSingleAsync(executor, query, token, action));
        }

        CommitCommand = new AsyncAutoCommand(async () =>
        {
            await Execute("COMMIT");
        }, () => !TaskRunning);
        RollbackCommand = new AsyncAutoCommand(async () =>
        {
            await Execute("ROLLBACK");
        }, () => !TaskRunning);
        ToggleIsAutoCommit = new AsyncAutoCommand(async () =>
        {
            await this.connection.SetAutoCommitAsync(!IsAutoCommit);
        }, () => !TaskRunning);
        ExecuteAllCommand = new AsyncAutoCommand(ExecuteAllAsync, () => !TaskRunning);
        ExecuteSelectedCommand = new AsyncAutoCommand(ExecuteSelectedAsync, () => !TaskRunning);
        BeautifyCommand = new AsyncAutoCommand(async () =>
        {
            if (languageServerInstance == null)
                return;
            
            try
            {
                IsEnabled = false;
                await mySqlSession.ScheduleAsync(async (token, _) =>
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
        }, () => !TaskRunning);
        
        StopQueryExecutionCommand = new AsyncAutoCommand(async () =>
        {
            if (mySqlSession.AnyTaskInSession())
                await mySqlSession.CancelAllAsync();
            else
            {
                if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Question")
                        .SetMainInstruction("Do you want to cancel all tasks in the connection?")
                        .SetContent("The current running task doesn't belong to this tab, however it does belong to some another tab which shares the same connection. Do you want to stop all tasks in the connection?")
                        .WithYesButton(true)
                        .WithCancelButton(false)
                        .Build()))
                {
                    await connection.CancelAllAsync();
                
                }
            }
        }, () => TaskRunning);
        
        StopAllTasksInConnectionCommand = new AsyncAutoCommand(async () =>
        {
            await connection.CancelAllAsync();
        }, () => TaskRunning);
        
        On(() => TaskRunning, _ =>
        {
            ExecuteAllCommand.RaiseCanExecuteChanged();
            ExecuteSelectedCommand.RaiseCanExecuteChanged();
            BeautifyCommand.RaiseCanExecuteChanged();
            StopQueryExecutionCommand.RaiseCanExecuteChanged();
            StopAllTasksInConnectionCommand.RaiseCanExecuteChanged();
            CommitCommand.RaiseCanExecuteChanged();
            RollbackCommand.RaiseCanExecuteChanged();
            ToggleIsAutoCommit.RaiseCanExecuteChanged();
        });
        
        ConnectAsync().ListenErrors();
    }

    private async Task<bool> AskIfApplyAsync(SelectResultsViewModel results)
    {
        if (!results.IsModified)
            return true;

        var result = await MessageBoxService.ShowDialog(new MessageBoxFactory<int>()
            .SetTitle("Warning")
            .SetMainInstruction($"{results.Title} has pending changes. Do you want to apply them?")
            .SetContent("If you don't apply them, they will be lost.")
            .WithYesButton(0)
            .WithNoButton(1)
            .WithCancelButton(2)
            .Build());

        if (result == 0)
        {
            if (!await results.SaveAsync())
                return false;
            return true;
        }
        else if (result == 1)
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
        var tcs = new TaskCompletionSource();
        connectTask = tcs.Task;
        try
        {
            mySqlSession = await connection.OpenSessionAsync();
            if (IsDisposed)
                await mySqlSession.DisposeAsync();
            else
                AutoDispose(new ActionDisposable(() => mySqlSession.DisposeAsync().AsTask().ListenErrors()));
            IsConnecting = false;
        }
        catch (Exception e)
        {
            await MessageBoxService.SimpleDialog("Error", "Couldn't open MySQL connection", e.Message);
        }
        finally
        {
            tcs.SetResult();
            connectTask = null;
        }
    }

    public Task ExecuteAndOverrideResultsAsync(string query, SelectSingleTableViewModel result)
    {
        var action = actionsOutputService.Create(query);
        return mySqlSession.ScheduleAsync((token, executor) => ExecuteSingleAsync(executor, query, token, action, result));
    }

    private async Task ExecuteSelectedAsync()
    {
        if (connectTask != null)
            await connectTask;
        
        querySplitter.UpdateRangesNow(Document);
        if (!querySplitter.AreRangesValid)
            throw new Exception("Invalid ranges! Fatal error, canceling. Sorry, please report this.");

        var range = querySplitter.GetRange(caretOffset);
        if (!range.HasValue)
            return;

        var query = Document.GetText(range.Value.Start, range.Value.Length);
        var action = actionsOutputService.Create(query);

        await mySqlSession.ScheduleAsync((token, executor) => ExecuteSingleAsync(executor, query, token, action));
    }

    public async Task ExecuteAsync(IReadOnlyList<string> queries, bool continueOnError, bool throwOnError, bool rollbackOnError)
    {
        if (queries.Count == 0)
            return;

        if (connectTask != null)
            await connectTask;
        
        // we do it before schedule, so that the action is added to the UI list before the execution starts
        var action = actionsOutputService.Create(queries[0]);

        await mySqlSession.ScheduleAsync(async (token, executor) =>
        {
            for (var index = 0; index < queries.Count; index++)
            {
                var query = queries[index];
                
                if (index > 0) // index 0 was created before the loop
                    action = actionsOutputService.Create(queries[index]);
                
                try
                {
                    await ExecuteSingleAsync(executor, query, token, action);
                }
                catch (Exception e)
                {
                    if (rollbackOnError)
                    {
                        action = actionsOutputService.Create("ROLLBACK");
                        await ExecuteSingleAsync(executor, "ROLLBACK", token, action);
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
            await connectTask;
        
        querySplitter.UpdateRangesNow(Document);
        if (!querySplitter.AreRangesValid)
            throw new Exception("Invalid ranges! Fatal error, canceling. Sorry, please report this.");

        List<string> queries = new();
        foreach (var range in querySplitter.Ranges)
            queries.Add(Document.GetText(range.Start, range.Length));

        await ExecuteAsync(queries, this.continueOnError, false, false);
    }

    private async Task ExecuteSingleAsync(IMySqlQueryExecutor executor, 
        string query, 
        CancellationToken cancellationToken, 
        IActionOutput action,
        SelectSingleTableViewModel? result = null)
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
                        var tableInfo = await executor.GetTableColumnsAsync(select.From.Schema, select.From.Table, cancellationToken);
                        
                        if (CheckIfCancelled())
                            return;
                        
                        Results.Add(new SelectSingleTableViewModel(this, select.From.Table, in res, select, tableInfo));
                    }
                    else
                        Results.Add(new SelectResultsViewModel(this, $"Query {action.Index}", in res));
                    SelectedResult = Results[^1];   
                }
                else
                {
                    Debug.Assert(isSelect);
                    result.UpdateResults(in res);
                }
                action.Response = $"{res.AffectedRows} row(s) returned";
            }
            else
            {
                Debug.Assert(result == null);
                
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
        if (!preferences.UseCodeCompletion)
        {
            LanguageServerState = LanguageServerStateEnum.Disabled;
            return;
        }
        
        if (languageServerStarting)
            return;

        try
        {
            languageServerStarting = true;
            var connId = await languageServer.ConnectAsync(connection.ConnectionData.Credentials);
            
            if (IsDisposed)
                return;
            
            languageServerInstance = await languageServer.NewFileAsync(connId);

            if (IsDisposed)
            {
                languageServerInstance.Dispose();
                return;
            }

            AutoDispose(languageServerInstance);
            
            AutoDispose(languageServerInstance.ServerAlive.SubscribeAction(isAlive =>
            {
                if (!isAlive)
                {
                    LanguageServerState = LanguageServerStateEnum.Crashed;
                }
                else
                {
                    if (LanguageServerState is LanguageServerStateEnum.Crashed or LanguageServerStateEnum.NotStarted)
                        LanguageServerState = LanguageServerStateEnum.Healthy;
                }
            }));
        }
        catch (Exception e)
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

        if (Document.TextLength > MaxDocumentLength)
        {
            LanguageServerState = LanguageServerStateEnum.PausedDueToTooLongDocument;
            return;
        }
        else if (LanguageServerState == LanguageServerStateEnum.PausedDueToTooLongDocument)
            LanguageServerState = LanguageServerStateEnum.Healthy;
        
        languageServerInstance.UpdateText(Document.Text);
        await ComputeCompletionAsync();

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
    
    private async Task ComputeCompletionAsync()
    {
        if (languageServerInstance == null)
            return;
        
        try
        {
            if (CaretOffset - 1 >= 0 && Document.GetCharAt(CaretOffset - 1) is var ch && (ch == ';' || char.IsWhiteSpace(ch)))
            {
                Completions.Clear();
                return;
            }

            if (CaretOffset < Document.TextLength && Document.GetCharAt(CaretOffset) is not ' ' and not '\n' and not '\r' and not '\t')
            {
                Completions.Clear();
                return;
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
            Console.WriteLine(e);
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
    public bool IsModified => isDocumentModified || IsAnyResultModified;
    private bool IsAnyResultModified => Results.Any(x => x.IsModified);
    [Notify] [AlsoNotify(nameof(IsModified))] private bool isDocumentModified;
    public ImageUri? Icon { get; } = new ImageUri("Icons/document_sql.png");
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public IObservable<IReadOnlyList<IInspectionResult>> Problems => problems;
    private ReactiveProperty<IReadOnlyList<IInspectionResult>> problems = new ReactiveProperty<IReadOnlyList<IInspectionResult>>(new List<IInspectionResult>());
    private DoubleBufferedValue<List<IInspectionResult>> problemsList = new DoubleBufferedValue<List<IInspectionResult>>();
}