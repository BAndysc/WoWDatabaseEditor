using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.QueryParser;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;

namespace WDE.TrinitySmartScriptEditor.Providers;

public class SmartScriptQueryParser : IQueryParserProvider
{
    public Task<bool> ParseDelete(DeleteQuery query, IQueryParsingContext context)
    {
        return Task.FromResult(query.TableName == DatabaseTable.WorldTable("smart_scripts"));
    }

    public async Task<bool> ParseInsert(InsertQuery query, IQueryParsingContext context)
    {
        if (query.TableName != DatabaseTable.WorldTable("smart_scripts"))
            return false;

        var entry = query.Columns.IndexOfIgnoreCase("entryorguid");
        var scriptSourceType = query.Columns.IndexOfIgnoreCase("source_type");

        if (entry == -1 || scriptSourceType == -1)
            return false;

        HashSet<(int, SmartScriptType)> pairs = new();
        foreach (var insert in query.Inserts)
        {
            if (insert[entry] is long entryValue &&
                insert[scriptSourceType] is long scriptSourceTypeValue)
            {
                pairs.Add(((int)entryValue, (SmartScriptType)scriptSourceTypeValue));
            }
        }
        
        foreach (var pair in pairs)
            context.ProduceItem(new SmartScriptSolutionItem(pair.Item1, pair.Item2));
        
        return true;
    }

    public Task<bool> ParseUpdate(UpdateQuery query, IQueryParsingContext context)
    {
        return Task.FromResult(query.TableName == DatabaseTable.WorldTable("smart_scripts"));
    }

    public void Finish(IQueryParsingContext context)
    {
    }
}