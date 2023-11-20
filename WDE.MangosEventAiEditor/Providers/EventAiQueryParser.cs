using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.QueryParser;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Utils;
using WDE.EventAiEditor;

namespace WDE.MangosEventAiEditor.Providers;

public class EventAiQueryParser : IQueryParserProvider
{
    public Task<bool> ParseDelete(DeleteQuery query, IQueryParsingContext context)
    {
        return Task.FromResult(query.TableName == DatabaseTable.WorldTable("creature_ai_scripts"));
    }

    public async Task<bool> ParseInsert(InsertQuery query, IQueryParsingContext context)
    {
        if (query.TableName != DatabaseTable.WorldTable("creature_ai_scripts"))
            return false;

        var creatureId = query.Columns.IndexOfIgnoreCase("creature_id");

        if (creatureId == -1)
            return false;

        HashSet<int> creatureIds = new();
        foreach (var insert in query.Inserts)
        {
            if (insert[creatureId] is long entryValue)
                creatureIds.Add(((int)entryValue));
        }
        
        foreach (var id in creatureIds)
            context.ProduceItem(new EventAiSolutionItem(id));
        
        return true;
    }

    public Task<bool> ParseUpdate(UpdateQuery query, IQueryParsingContext context)
    {
        return Task.FromResult(query.TableName == DatabaseTable.WorldTable("creature_ai_scripts"));
    }

    public void Finish(IQueryParsingContext context)
    {
    }
}