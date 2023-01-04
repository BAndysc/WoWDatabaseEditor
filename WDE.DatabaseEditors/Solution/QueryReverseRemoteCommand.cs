using System;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.QueryParser;
using WDE.Common.Sessions;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution;

[SingleInstance]
[AutoRegister]
[ReverseRemoteCommand("query")]
public class QueryReverseRemoteCommand : IReverseRemoteCommand
{
    private readonly IEventAggregator eventAggregator;
    private readonly Lazy<IMySqlExecutor> mysqlExecutor;
    private readonly Lazy<IQueryParserService> queryParser;
    private readonly Lazy<ISessionService> sessionService;

    public QueryReverseRemoteCommand(IEventAggregator eventAggregator,
        Lazy<IMySqlExecutor> mysqlExecutor,
        Lazy<IQueryParserService> queryParser,
        Lazy<ISessionService> sessionService)
    {
        this.eventAggregator = eventAggregator;
        this.mysqlExecutor = mysqlExecutor;
        this.queryParser = queryParser;
        this.sessionService = sessionService;
    }
        
    public async Task Invoke(ICommandArguments arguments)
    {
        var query = arguments.TakeRestArguments;
        LOG.LogInformation(query);

        var items = await queryParser.Value.GenerateItemsForQuery(query);
        
        foreach (var e in items.errors)
            LOG.LogError(e);
        
        await mysqlExecutor.Value.ExecuteSql(query);
        
        foreach (var item in items.items)
            await sessionService.Value.UpdateQuery(item);
    }

    public bool BringEditorToFront => false;
}