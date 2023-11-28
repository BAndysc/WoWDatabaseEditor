using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Prism.Commands;
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
    private const int MaxDocumentLength = 500_000;
    public enum LanguageServerStateEnum
    {
        Healthy,
        NotInstalled,
        Crashed,
        PausedDueToTooLongDocument,
        Disabled
    }
    
    private readonly IMySqlConnector connector;
    private readonly IActionsOutputService actionsOutputService;
    private readonly DatabaseConnectionData connectionData;
    [Notify] [AlsoNotify(nameof(CursorStatus))] private int caretOffset;
    [Notify] [AlsoNotify(nameof(CursorStatus))] private int caretLine = 1;
    [Notify] [AlsoNotify(nameof(CursorStatus))] private int caretColumn = 1;

    private readonly ISqlLanguageServer languageServer;
    private readonly IQueryUtility queryUtility;
    private readonly ISqlWorkbenchPreferences preferences;
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
    public IAsyncCommand BeautifyCommand { get; }
    public DelegateCommand<SelectResultsViewModel> CloseTabCommand { get; }
    public DelegateCommand CloseAllTabsCommand { get; }
    public DelegateCommand<SelectResultsViewModel> CloseOtherTabsCommand { get; }
    public DelegateCommand<SelectResultsViewModel> CloseLeftTabsCommand { get; }
    public DelegateCommand<SelectResultsViewModel> CloseRightTabsCommand { get; }
    public ICommand CommitCommand { get; }
    public ICommand RollbackCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public IAsyncCommand RestartLanguageServerCommand { get; }

    [Notify] private bool isAutoCommit = true;
    [Notify] private bool showNonPrintableChars = false;
    [Notify] private bool wordWrap = false;
    [Notify] private int limit = 10_000;
    [Notify] private bool continueOnError = false;
    
    [Notify] private SelectResultsViewModel? selectedResult;
    
    public ObservableCollection<SelectResultsViewModel> Results { get; } =
        new ObservableCollection<SelectResultsViewModel>();

    public bool HasAnyResults => Results.Count > 0;
    
    public string CursorStatus => $"line {CaretLine}, column {CaretColumn}, location {CaretOffset}";

    public ImageUri ConnectedToIcon => connectionData.Icon ?? ImageUri.Empty;
    
    public bool IsSessionOpened => mySqlSession != null && mySqlSession.IsSessionOpened;
    
    public string ConnectedTo => connectionData.ConnectionName;

    public Color? ConnectedToColor => connectionData.Color;
    
    public List<int> Limits { get; } = new List<int>() { 100, 1_000, 5_000, 10_000, 50_000, 500_000 };
    
    [Notify] private bool isEnabled = true;

    [Notify] [AlsoNotify(nameof(LanguageServerToolTipText), nameof(LanguageServerHealthy))] private LanguageServerStateEnum languageServerState;
    
    public bool LanguageServerHealthy => languageServerState == LanguageServerStateEnum.Healthy;
    
    public bool TaskRunning => taskQueue.IsRunning;
    
    public string LanguageServerToolTipText => languageServerState switch
    {
        LanguageServerStateEnum.Healthy => "Code completions seem to be working, but if they are not, press the button to restart the language server.",
        LanguageServerStateEnum.NotInstalled => "Code completions don't work, because SQLS is not found, if you have downloaded the editor binary, please report the error, if you are using Linux, macOS or built the editor manually, please download the SQLS and provide the path in the settings.",
        LanguageServerStateEnum.Crashed => "Code completions have crashed few times in a row and were disabled. You can try to restart the language server by pressing the button.",
        LanguageServerStateEnum.PausedDueToTooLongDocument => "Code completions are now paused, because your document is too long.",
        LanguageServerStateEnum.Disabled => "Code completions are disabled in the settings.",
        _ => throw new ArgumentOutOfRangeException()
    };
    
    private IMySqlSession? mySqlSession;

    private TaskQueue taskQueue = new();
    
    public SqlWorkbenchViewModel(IMySqlConnector connector,
        IActionsOutputService actionsOutputService,
        ISqlLanguageServer languageServer,
        IConfigureService configureService,
        IQueryUtility queryUtility,
        IMessageBoxService messageBoxService,
        ISqlWorkbenchPreferences preferences,
        DatabaseConnectionData connectionData)
    {
        MessageBoxService = messageBoxService;
        this.connector = connector;
        this.actionsOutputService = actionsOutputService;
        this.connectionData = connectionData;
        this.languageServer = languageServer;
        this.queryUtility = queryUtility;
        this.preferences = preferences;
        Document = new TextDocument();
        textMarkerService = new TextMarkerService(Document);
        Document.UpdateStarted += DocumentUpdateStarted;
        Document.UpdateFinished += DocumentUpdateFinished;
        title = "New query";
        ConnectLanguageServerAsync().ListenErrors();
        Document.UndoStack.ToObservable(x => x.IsOriginalFile)
            .SubscribeAction(@is => IsDocumentModified = !@is);

        Results.CollectionChanged += (_, _) => RaisePropertyChanged(nameof(HasAnyResults));
        taskQueue.ToObservable(x => x.IsRunning).SubscribeAction(_ => RaisePropertyChanged(nameof(TaskRunning)));

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
            
            configViewModel.SelectedConnection = configViewModel.Connections.FirstOrDefault(x => x.Id == connectionData.Id);
        });
        
        CloseTabCommand = new DelegateCommand<SelectResultsViewModel>(r =>
        {
            var indexOf = Results.IndexOf(r);
            if (indexOf >= 0)
            {
                var wasSelected = r == SelectedResult;
                Results.Remove(r);
                if (wasSelected)
                    SelectedResult = Results.Count > indexOf ? Results[indexOf] : Results.LastOrDefault();
            }
        });
        
        CloseAllTabsCommand = new DelegateCommand(() =>
        {
            Results.Clear();
            SelectedResult = null;
        });
        
        CloseOtherTabsCommand = new DelegateCommand<SelectResultsViewModel>(r =>
        {
            Results.RemoveIf(x => x != r);
            SelectedResult = r;
        });
        
        CloseLeftTabsCommand = new DelegateCommand<SelectResultsViewModel>(r =>
        {
            var index = Results.IndexOf(r);
            var selectedIndex = SelectedResult == null ? -1 : Results.IndexOf(SelectedResult);
            if (index >= 0)
            {
                for (int i = index - 1; i >= 0; --i)
                    Results.RemoveAt(i);
                if (selectedIndex < index)
                    SelectedResult = Results.Count > 0 ? Results[0] : null;
            }
        });
        
        CloseRightTabsCommand = new DelegateCommand<SelectResultsViewModel>(r =>
        {
            var index = Results.IndexOf(r);
            var selectedIndex = SelectedResult == null ? -1 : Results.IndexOf(SelectedResult);
            if (index >= 0)
            {
                while (Results.Count > index + 1)
                    Results.RemoveAt(index + 1);
                if (selectedIndex > index)
                    SelectedResult = index < Results.Count ? Results[index] : null;
            }
        });
        
        RestartLanguageServerCommand = new AsyncAutoCommand(RestartLanguageServerAsync);
        
        Task Execute(string query)
        {
            return taskQueue.Schedule(async token =>
            {
                await ExecuteSingleAsync(query, token);
            });
        }

        CommitCommand = new AsyncAutoCommand(async () =>
        {
            await Execute("COMMIT");
        });
        RollbackCommand = new AsyncAutoCommand(async () =>
        {
            await Execute("ROLLBACK");
        });
        ExecuteAllCommand = new AsyncAutoCommand(() =>
        {
            return taskQueue.Schedule(ExecuteAllAsync);
        }, () => !TaskRunning);
        ExecuteSelectedCommand = new AsyncAutoCommand(() =>
        {
            return taskQueue.Schedule(ExecuteSelectedAsync);
        }, () => !TaskRunning);
        BeautifyCommand = new AsyncAutoCommand(async () =>
        {
            if (languageServerInstance == null)
                return;
            
            try
            {
                IsEnabled = false;
                await taskQueue.Schedule(async token =>
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
            await taskQueue.CancelAll();
        }, () => TaskRunning);
        
        this.On(() => TaskRunning, _ =>
        {
            ExecuteAllCommand.RaiseCanExecuteChanged();
            ExecuteSelectedCommand.RaiseCanExecuteChanged();
            BeautifyCommand.RaiseCanExecuteChanged();
            StopQueryExecutionCommand.RaiseCanExecuteChanged();
        });
        this.ToObservable(x => x.IsAutoCommit)
            .Skip(1) // auto commit is a default, no need to execute it
            .SubscribeAction(@is =>
            {
                Execute(@is ? "SET autocommit = ON" : "SET autocommit = OFF").ListenErrors();
            });
    }

    public Task ExecuteAndOverrideResultsAsync(string query, SelectSingleTableViewModel result)
    {
        return taskQueue.Schedule(token => ExecuteSingleAsync(query, token, result));
    }

    private async Task ExecuteSelectedAsync(CancellationToken cancellationToken)
    {
        querySplitter.UpdateRangesNow(Document);
        if (!querySplitter.AreRangesValid)
            throw new Exception("Invalid ranges! Fatal error, canceling. Sorry, please report this.");

        var range = querySplitter.GetRange(caretOffset);
        if (!range.HasValue)
            return;

        if (cancellationToken.IsCancellationRequested)
            return;
        
        await ExecuteSingleAsync(Document.GetText(range.Value.Start, range.Value.Length), cancellationToken);
    }

    private async Task ExecuteAllAsync(CancellationToken cancellationToken)
    {
        querySplitter.UpdateRangesNow(Document);
        if (!querySplitter.AreRangesValid)
            throw new Exception("Invalid ranges! Fatal error, canceling. Sorry, please report this.");

        List<string> queries = new();
        foreach (var range in querySplitter.Ranges)
            queries.Add(Document.GetText(range.Start, range.Length));

        foreach (var query in queries)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                await ExecuteSingleAsync(query, cancellationToken);
            }
            catch (Exception e)
            {
                if (!continueOnError)
                    break;
            }
        }
    }
    
    private async Task ExecuteSingleAsync(string query, CancellationToken cancellationToken, SelectSingleTableViewModel? result = null)
    {
        var action = actionsOutputService.Create(query);

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
            mySqlSession ??= AutoDispose(await connector.ConnectAsync(connectionData.Credentials));

            if (CheckIfCancelled())
                return;
            
            if (!mySqlSession.IsSessionOpened)
            {
                await mySqlSession.TryReconnectAsync();
                if (CheckIfCancelled())
                    return;
            }

            var res = await mySqlSession.ExecuteSqlAsync(query, limit, cancellationToken);
            action.TimeFinished = DateTime.Now;

            if (CheckIfCancelled())
                return;

            if (!res.IsNonQuery)
            {
                var isSelect = queryUtility.IsSimpleSelect(query, out var select);
                if (result == null)
                {
                    if (isSelect)
                    {
                        var tableInfo = await mySqlSession.GetTableColumnsAsync(select.From, cancellationToken);
                        
                        if (CheckIfCancelled())
                            return;
                        
                        Results.Add(new SelectSingleTableViewModel(this, select.From, in res, select, tableInfo));
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
    
    private async Task ConnectLanguageServerAsync()
    {
        if (!preferences.UseCodeCompletion)
        {
            LanguageServerState = LanguageServerStateEnum.Disabled;
            return;
        }
        try
        {
            var connId = await languageServer.ConnectAsync(connectionData.Credentials);
            languageServerInstance = AutoDispose(await languageServer.NewFileAsync(connId));
            AutoDispose(languageServerInstance.ServerAlive.SubscribeAction(isAlive =>
            {
                if (!isAlive)
                {
                    LanguageServerState = LanguageServerStateEnum.Crashed;
                }
                else
                {
                    if (LanguageServerState == LanguageServerStateEnum.Crashed)
                        LanguageServerState = LanguageServerStateEnum.Healthy;
                }
            }));
        }
        catch (Exception e)
        {
            LanguageServerState = LanguageServerStateEnum.NotInstalled;
            throw;
        }
    }

    private void DocumentUpdateFinished(object? sender, EventArgs e)
    {
        previousTimer?.Dispose();
        previousTimer = DispatcherTimer.RunOnce(() =>
        {
            var snapshot = Document.CreateSnapshot();
            currentProcessing?.Cancel();
            currentProcessing = new CancellationTokenSource();
            ProcessDataAsync(snapshot, currentProcessing.Token).ListenErrors();
        }, TimeSpan.FromMilliseconds(250));
    }
    
    private void DocumentUpdateStarted(object? sender, EventArgs e)
    {
        querySplitter.InvalidateRanges();
        UpdateQueryMarker();
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
        marker.BackgroundBrush = new SolidColorBrush(new Color(30, 0, 0, 0));
    }

    private async Task ProcessDataAsync(ITextSource snapshot, CancellationToken token)
    {
        if (!await querySplitter.UpdateRangesAsync(snapshot))
            return;
        
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
    public bool IsModified => isDocumentModified;
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