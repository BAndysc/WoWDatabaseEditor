using System.Collections.Generic;

namespace WDE.SqlQueryGenerator
{
    public interface IUpdateQuery
    {
        public IWhere Condition { get; }
        public IEnumerable<(string, string)> Updates { get; }
    }
}