using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IConditionQueryGenerator
    {
        IQuery BuildDeleteQuery(IDatabaseProvider.ConditionKey conditionKey);
        IQuery BuildInsertQuery(IReadOnlyList<IConditionLine> conditions);
    }
}