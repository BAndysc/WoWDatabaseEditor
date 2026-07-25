using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IMangosConditionQueryGenerator
    {
        IQuery BuildDeleteQuery(IReadOnlyList<uint> entries);
        IQuery BuildInsertQuery(IReadOnlyList<IMangosConditionLine> conditions);
    }
}
