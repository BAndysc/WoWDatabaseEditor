using System.Collections.Generic;
using System.Linq;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Conditions.Exporter
{
    [AutoRegister]
    public class ConditionQueryGenerator : IConditionQueryGenerator
    {
        private readonly ICurrentCoreVersion currentCoreVersion;

        public ConditionQueryGenerator(ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
        }
        
        public IQuery BuildDeleteQuery(IDatabaseProvider.ConditionKey key)
        {
            return Queries.Table(DatabaseTable.WorldTable("conditions"))
                .Where(row => row.Column<int>("SourceTypeOrReferenceId") == key.SourceType &&
                              (!key.SourceGroup.HasValue || row.Column<int>("SourceGroup") == key.SourceGroup.Value) &&
                              (!key.SourceEntry.HasValue || row.Column<int>("SourceEntry") == key.SourceEntry.Value) &&
                              (!key.SourceId.HasValue || row.Column<int>("SourceId") == key.SourceId.Value))
                .Delete();
        }

        public IQuery BuildInsertQuery(IReadOnlyList<IConditionLine> conditions)
        {
            return Queries.Table(DatabaseTable.WorldTable("conditions"))
                .BulkInsert(conditions.Select(c =>
                {
                    if (currentCoreVersion.Current.ConditionFeatures.HasConditionStringValue)
                        return c.ToSqlObjectMaster();
                    return c.ToSqlObject();
                }));
        }
    }
}
