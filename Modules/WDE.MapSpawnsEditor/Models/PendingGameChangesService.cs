using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.QueryParser;
using WDE.Common.Sessions;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawnsEditor.Models;

[UniqueProvider]
public interface IPendingGameChangesService
{
    bool HasAnyPendingChanges();
    bool HasGuidPendingChange(GuidType type, uint entry, uint guid);
    void AddQuery(GuidType type, uint entry, uint guid, IQuery query);
    void AddGlobalQuery(IQuery query);
    List<(GuidType, uint entry, uint guid)> CancelAll();
    Task SaveAll();
    event Action? HasPendingChanged;
    event Action<IQuery>? QueryExecuted;
    event Action<List<(GuidType, uint entry, uint guid)>>? RequestReload;
}

[AutoRegister]
[SingleInstance]
public class PendingGameChangesService : IPendingGameChangesService
{
    private readonly IMessageBoxService messageBoxService;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IQueryParserService queryParserService;
    private readonly ISessionService sessionService;
    private List<IQuery> pendingChanges = new();
    private List<IQuery> globalPendingChanges = new();
    private HashSet<(GuidType, uint entry, uint guid)> pendingGuidChanges = new();

    public event Action<IQuery>? QueryExecuted;
    public event Action<List<(GuidType, uint entry, uint guid)>>? RequestReload;
    public event Action? HasPendingChanged;

    public PendingGameChangesService(IMessageBoxService messageBoxService,
        IMySqlExecutor mySqlExecutor,
        IQueryParserService queryParserService,
        ISessionService sessionService)
    {
        this.messageBoxService = messageBoxService;
        this.mySqlExecutor = mySqlExecutor;
        this.queryParserService = queryParserService;
        this.sessionService = sessionService;
    }
    
    public bool HasAnyPendingChanges()
    {
        return pendingGuidChanges.Count != 0 || globalPendingChanges.Count != 0;
    }

    public bool HasGuidPendingChange(GuidType type, uint entry, uint guid)
    {
        return globalPendingChanges.Count > 0 || pendingGuidChanges.Contains((type, entry, guid));
    }

    public void AddQuery(GuidType type, uint entry, uint guid, IQuery query)
    {
        pendingGuidChanges.Add((type, entry, guid));
        pendingChanges.Add(query);
        HasPendingChanged?.Invoke();
    }

    public void AddGlobalQuery(IQuery query)
    {
        globalPendingChanges.Add(query);
    }

    public List<(GuidType, uint entry, uint guid)> CancelAll()
    {
        var list = pendingGuidChanges.ToList();
        pendingChanges.Clear();
        pendingGuidChanges.Clear();
        globalPendingChanges.Clear();
        HasPendingChanged?.Invoke();
        return list;
    }

    private List<IQuery> Flush(out List<(GuidType, uint entry, uint guid)> toReload)
    {
        toReload = pendingGuidChanges.ToList();
        var list = globalPendingChanges;
        list.AddRange(pendingChanges);
        globalPendingChanges = new();
        pendingGuidChanges.Clear();
        pendingChanges.Clear();
        HasPendingChanged?.Invoke();
        return list;
    }

    public async Task SaveAll()
    {
        var toExecute = Flush(out var toReload);
        IList<ISolutionItem>? solutionItems = null;
        IList<string>? errors = null;

        foreach (var query in toExecute)
        {
            if (sessionService.IsOpened && !sessionService.IsPaused)
                (solutionItems, errors) = await queryParserService.GenerateItemsForQuery(query.QueryString);

            await mySqlExecutor.ExecuteSql(query);
            QueryExecuted?.Invoke(query);

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
        }

        RequestReload?.Invoke(toReload);
    }
}