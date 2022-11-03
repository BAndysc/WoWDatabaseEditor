using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Conditions;

[AutoRegister]
public class ConditionDeleteProvider : IDeleteQueryProvider<ConditionDeleteModel>
{
    public IQuery Delete(ConditionDeleteModel t)
    {
        List<long>? list =  t.BySourceEntry ?? (t.BySourceGroup ?? t.BySourceId);
        var columnKey = t.BySourceEntry == null ? (t.BySourceGroup == null ? t.BySourceId == null ? null : "SourceId" : "SourceGroup") : "SourceEntry";
        
        if (columnKey == null || list == null)
            return Queries.Empty();

        return Queries.Table("conditions")
            .Where(r => r.Column<int>("SourceTypeOrReferenceId") == t.SourceTypeOrReferenceId)
            .WhereIn(columnKey, list)
            .Delete();
    }
}