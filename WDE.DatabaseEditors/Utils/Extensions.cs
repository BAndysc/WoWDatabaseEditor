using System.Collections.Generic;
using System.Linq;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;

namespace WDE.DatabaseEditors.Utils;

public static class Extensions
{
    public static List<EntityOrigianlField>? GetOriginalFields(this DatabaseEntity entity)
    {
        if (!entity.ExistInDatabase)
            return null;
            
        var modified = entity.Fields.Where(f => f.IsModified).ToList();
        if (modified.Count == 0)
            return null;
            
        return modified.Select(f => new EntityOrigianlField()
            {ColumnName = f.FieldName, OriginalValue = f.OriginalValue}).ToList();
    }
}