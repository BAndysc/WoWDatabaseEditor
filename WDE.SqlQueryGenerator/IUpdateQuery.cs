using System.Collections.Generic;
using System.Linq;

namespace WDE.SqlQueryGenerator
{
    public interface IUpdateQuery
    {
        public enum Operator
        {
            Set,
            SetOr
        }
        
        public IWhere Condition { get; }
        public IReadOnlyList<(string column, string value, Operator op, string? comment)> Updates { get; }
        bool Empty => !Updates.Any();
    }
}