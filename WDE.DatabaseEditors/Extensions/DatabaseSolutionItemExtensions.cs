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
                        {
                            // there is a chance original.ColumnName comes from an old version of the json, where the foreign table name was not included
                            // so we need to try to find the cell by name only, this is the case for example for creature_template and creature_template_addon
                            cell = entity.Cells.FirstOrDefault(pair => pair.Key.ForeignTable != null && pair.Key.ColumnName == original.ColumnName.ColumnName).Value;
                        }

                        if (cell == null)
                            continue;

                        cell.OriginalValue = original.OriginalValue;
                    }
            }
        }
    }
}