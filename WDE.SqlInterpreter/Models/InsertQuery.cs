using System.Collections.Generic;

namespace WDE.SqlInterpreter.Models
{
    public interface IBaseQuery
    {
        string TableName { get; }
    }

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

    public class UpdateQuery : IBaseQuery
    {
        public UpdateQuery(string tableName, IReadOnlyList<UpdateElement> updates, SimpleWhereCondition where)
        {
            TableName = tableName;
            Updates = updates;
            Where = where;
        }

        public string TableName { get; }
        public IReadOnlyList<UpdateElement> Updates { get; }
        public SimpleWhereCondition Where { get; }
    }

    public class SimpleWhereCondition
    {
        public SimpleWhereCondition(string columnName, IReadOnlyList<object?> values)
        {
            ColumnName = columnName;
            Values = values;
        }

        public string ColumnName { get; }
        public IReadOnlyList<object?> Values { get; }
    }

    public class UpdateElement
    {
        public UpdateElement(string columnName)
        {
            ColumnName = columnName;
        }

        public string ColumnName { get; }
    }
}