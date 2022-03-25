using System.Collections.Generic;
using WDE.Common.Services.QueryParser.Models;
using WDE.Module.Attributes;

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