using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.QueryParser;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Services.TextDocumentService;

[SingleInstance]
[AutoRegister]
public class TextDocumentService : ITextDocumentService
{
    private readonly Func<TextDocumentViewModel> creator;
    private readonly IWindowManager windowManager;
    private readonly IMessageBoxService messageBoxService;
    private readonly Lazy<IStatusBar> statusBar;
    private readonly ISessionService sessionService;
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITaskRunner taskRunner;
    private readonly IQueryParserService queryParserService;
    private readonly IMySqlExecutor mySqlExecutor;

    public TextDocumentService(Func<TextDocumentViewModel> creator, IWindowManager windowManager,
        IMessageBoxService messageBoxService,
        Lazy<IStatusBar> statusBar,
        ISessionService sessionService,
        IDatabaseProvider databaseProvider,
        ITaskRunner taskRunner,
        IQueryParserService queryParserService,
        IMySqlExecutor mySqlExecutor)
    {
        this.creator = creator;
        this.windowManager = windowManager;
        this.messageBoxService = messageBoxService;
        this.statusBar = statusBar;
        this.sessionService = sessionService;
        this.databaseProvider = databaseProvider;
        this.taskRunner = taskRunner;
        this.queryParserService = queryParserService;
        this.mySqlExecutor = mySqlExecutor;
    }
        
    public Task ExecuteSqlSaveSession(string query, bool inspectQuery)
    {
        return taskRunner.ScheduleTask("Executing query",
            () => WrapStatusbar(async () =>
            {
                IList<ISolutionItem>? solutionItems = null;
                IList<string>? errors = null;
                if (inspectQuery && sessionService.IsOpened && !sessionService.IsPaused)
                {
                    (solutionItems, errors) = await queryParserService.GenerateItemsForQuery(query);
                }

                await mySqlExecutor.ExecuteSql(query);

                if (solutionItems != null)
                {
                    foreach (var item in solutionItems)
                        await sessionService.UpdateQuery(item);
                    if (errors!.Count > 0)
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Apply query")
                            .SetMainInstruction("Some queries couldn't be transformed into session items")
                            .SetContent("Details:\n\n" + string.Join("\n", errors.Select(s => "  - " + s)))
                            .WithOkButton(true)
                            .Build());
                    }
                }
            }));
    }

    public Task ExecuteSql(string sql)
    {
        return taskRunner.ScheduleTask("Executing query",
            () => WrapStatusbar(() => mySqlExecutor.ExecuteSql(sql)));
    }
        
    private async Task WrapStatusbar(Func<Task> action)
    {
        statusBar.Value.PublishNotification(new PlainNotification(NotificationType.Info, "Executing query"));
        try
        {
            await action();
            statusBar.Value.PublishNotification(new PlainNotification(NotificationType.Success, "Query executed"));
        }
        catch (Exception e)
        {
            statusBar.Value.PublishNotification(new PlainNotification(NotificationType.Error, "Failure during query execution: " + e.Message));
            LOG.LogError(e);
        }
    }

    public IDocument CreateDocument(string title, string text, string extension, bool inspectQuery = false)
    {
        var doc = creator();
        doc.Set(title, text, extension, inspectQuery);
        return doc;
    }
}