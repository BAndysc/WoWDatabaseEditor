using System.Collections.Generic;
using System.Linq;

namespace WDE.DbScriptsEditor.Models
{
    // Pure builder for "which of these ids exist" batch queries used by the async referential
    // inspections. Values are numeric and table/column names are whitelisted, so interpolation is
    // injection-safe.
    public static class DbScriptExistenceQuery
    {
        // SELECT DISTINCT `<idColumn>` FROM `<table>` WHERE `<idColumn>` IN (v1, v2, ...) [AND <extraWhere>].
        // Null when there is nothing to check. extraWhere must be a trusted literal (never user input).
        public static string? Build(string tableName, string idColumn, IReadOnlyCollection<long> ids, string? extraWhere = null)
        {
            if (ids.Count == 0)
                return null;
            var inList = string.Join(",", ids.Distinct().OrderBy(v => v));
            var extra = extraWhere == null ? "" : $" AND {extraWhere}";
            return $"SELECT DISTINCT `{idColumn}` FROM `{tableName}` WHERE `{idColumn}` IN ({inList}){extra}";
        }
    }
}
