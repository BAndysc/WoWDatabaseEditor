using System.Collections.Generic;
using System.Linq;

namespace WDE.DbScriptsEditor.Models
{
    // Pure helpers for the "find references to an entity" query used by find-anywhere / related
    // items. Kept free of DB/UI deps so the SQL shape can be unit-tested. Values are numeric and
    // columns/tables are whitelisted (enum-derived), so string interpolation is injection-safe.
    public static class DbScriptReferenceQuery
    {
        // Which long/int columns carry a value of one of the given parameter types, and for which
        // commands. E.g. searching a creature entry -> { DataLong: {10, 41}, DataInt2: {37} }.
        public static Dictionary<DbScriptDestination, HashSet<uint>> MapColumns(
            IEnumerable<DbScriptCommandDefinition> commands, IReadOnlyList<string> parameterTypeNames)
        {
            var map = new Dictionary<DbScriptDestination, HashSet<uint>>();
            var wanted = new HashSet<string>(parameterTypeNames);

            void Consider(uint commandId, IEnumerable<DbScriptCommandParameter> parameters)
            {
                foreach (var p in parameters)
                {
                    if (DbScriptDestinations.IsFloat(p.Destination))
                        continue; // entity ids never live in float columns
                    if (!wanted.Contains(p.Type))
                        continue;
                    if (!map.TryGetValue(p.Destination, out var set))
                        map[p.Destination] = set = new HashSet<uint>();
                    set.Add(commandId);
                }
            }

            foreach (var command in commands)
            {
                Consider(command.Id, command.Parameters);
                foreach (var variant in command.Variants)
                {
                    if (variant.Parameters != null)
                        Consider(command.Id, variant.Parameters);
                }
            }

            return map;
        }

        // SELECT of distinct script ids in one table that reference the value in one of the mapped
        // columns (restricted to the commands that actually use that column). Null if nothing maps.
        public static string? BuildTableQuery(
            string tableName, IReadOnlyDictionary<DbScriptDestination, HashSet<uint>> columnToCommands, long value)
        {
            var clauses = new List<string>();
            foreach (var (dest, commands) in columnToCommands)
            {
                if (commands.Count == 0)
                    continue;
                var col = DbScriptDestinations.ColumnName(dest);
                var cmdList = string.Join(",", commands.OrderBy(c => c));
                clauses.Add($"(`command` IN ({cmdList}) AND `{col}` = {value})");
            }
            if (clauses.Count == 0)
                return null;
            return $"SELECT DISTINCT `id` FROM `{tableName}` WHERE {string.Join(" OR ", clauses)}";
        }
    }
}
