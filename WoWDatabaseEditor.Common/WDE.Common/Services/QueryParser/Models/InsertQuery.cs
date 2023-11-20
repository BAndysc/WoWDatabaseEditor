using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.Common.Services.QueryParser.Models
{
    public class InsertQuery : IBaseQuery
    {
        public InsertQuery(DatabaseTable tableName, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object>> inserts)
        {
            TableName = tableName;
            Columns = columns;
            Inserts = inserts;
        }

        public DatabaseTable TableName { get; }
        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<IReadOnlyList<object>> Inserts { get; }
    }
}