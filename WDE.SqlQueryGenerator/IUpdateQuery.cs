using System.Collections.Generic;
using System.Linq;

namespace WDE.SqlQueryGenerator
{
    public interface IUpdateQuery
    {
        public IWhere Condition { get; }
        public IEnumerable<(string, string)> Updates { get; }
        bool Empty => !Updates.Any();
    }
}