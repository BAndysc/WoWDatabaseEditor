using System.Collections.Generic;

namespace WDE.SqlInterpreter.Models
{
    public class InsertQuery : IBaseQuery
    {
        public InsertQuery(string tableName, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object>> inserts)
        {
            TableName = tableName;
            Columns = columns;
            Inserts = inserts;
        }

        public string TableName { get; }
        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<IReadOnlyList<object>> Inserts { get; }
    }
}