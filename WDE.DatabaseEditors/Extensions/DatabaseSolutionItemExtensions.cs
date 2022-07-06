using System.Collections.Generic;
using System.Linq;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;

namespace WDE.DatabaseEditors.Extensions
{
    public static class DatabaseSolutionItemExtensions
    {
        public static void UpdateEntitiesWithOriginalValues(this DatabaseTableSolutionItem item,
            IReadOnlyList<DatabaseEntity> entities)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            foreach (var solutionEntity in item.Entries)
            {
                var entity = entities.FirstOrDefault(e => e.Key == solutionEntity.Key);
                if (entity == null)
                    continue;
                
                entity.ExistInDatabase = solutionEntity.ExistsInDatabase;
                entity.ConditionsModified = solutionEntity.ConditionsModified;
                
                if (solutionEntity.OriginalValues != null)
                    foreach (var original in solutionEntity.OriginalValues)
                    {
                        var cell = entity.GetCell(original.ColumnName);
                        if (cell == null)
                            continue;
                        cell.OriginalValue = original.OriginalValue;
                    }
            }
        }
    }
}