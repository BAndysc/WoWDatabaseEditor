using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IMangosUnitConditionQueryGenerator
    {
        IQuery BuildDeleteQuery(IReadOnlyList<int> ids);
        IQuery BuildInsertQuery(IReadOnlyList<IMangosUnitConditionLine> lines);
    }
}
