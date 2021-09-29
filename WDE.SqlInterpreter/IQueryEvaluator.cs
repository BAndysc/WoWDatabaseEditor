using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.SqlInterpreter.Models;

namespace WDE.SqlInterpreter
{
    [UniqueProvider]
    public interface IQueryEvaluator
    {
        IEnumerable<InsertQuery> ExtractInserts(string query);
        IEnumerable<UpdateQuery> ExtractUpdates(string query);
        IReadOnlyList<IBaseQuery> Extract(string query);
    }
}