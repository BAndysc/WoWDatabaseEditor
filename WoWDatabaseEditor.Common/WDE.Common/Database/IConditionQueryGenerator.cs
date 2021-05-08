using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IConditionQueryGenerator
    {
        string BuildDeleteQuery(IDatabaseProvider.ConditionKey conditionKey);
        string BuildInsertQuery(IList<IConditionLine> conditions);
    }
}