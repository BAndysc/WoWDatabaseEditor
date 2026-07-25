using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.QueryParser;
using WDE.Common.Services.QueryParser.Models;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Providers
{
    // Pasting a dbscripts_on_* SQL dump into the SQL-import flow opens the editor document
    // for each affected (table, id). Mirrors WDE.MangosEventAiEditor/Providers/EventAiQueryParser.
    public class DbScriptQueryParser : IQueryParserProvider
    {
        private static bool IsDbScriptTable(DatabaseTable table, out DbScriptType type)
        {
            type = default;
            return table.Database == DataDatabaseType.World &&
                   DbScriptTypes.TryFromTableName(table.Table, out type);
        }

        public Task<bool> ParseDelete(DeleteQuery query, IQueryParsingContext context)
        {
            return Task.FromResult(IsDbScriptTable(query.TableName, out _));
        }

        public Task<bool> ParseInsert(InsertQuery query, IQueryParsingContext context)
        {
            if (!IsDbScriptTable(query.TableName, out var type))
                return Task.FromResult(false);

            var idIndex = query.Columns.IndexOfIgnoreCase("id");
            if (idIndex == -1)
                return Task.FromResult(true);

            HashSet<uint> ids = new();
            foreach (var insert in query.Inserts)
            {
                if (insert[idIndex] is long idValue && idValue >= 0)
                    ids.Add((uint)idValue);
            }

            foreach (var id in ids)
                context.ProduceItem(new DbScriptSolutionItem(type, id));

            return Task.FromResult(true);
        }

        public Task<bool> ParseUpdate(UpdateQuery query, IQueryParsingContext context)
        {
            return Task.FromResult(IsDbScriptTable(query.TableName, out _));
        }

        public void Finish(IQueryParsingContext context)
        {
        }
    }
}
