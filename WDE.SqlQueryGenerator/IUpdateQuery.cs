using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    public interface IUpdateQuery
    {
        public enum Operator
        {
            Set,
            SetOr,
            SetAndNot
        }
        
        public IWhere Condition { get; }
        public IReadOnlyList<(string column, string value, Operator op, string? comment)> Updates { get; }
        bool Empty => !Updates.Any();
        public DataDatabaseType Database { get; }
    }
}