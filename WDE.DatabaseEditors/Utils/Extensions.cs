using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
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

    public static string FillTemplate(this DatabaseEntity entity, string template)
    {
        int indexOf = 0;
        indexOf = template.IndexOf("{", indexOf, StringComparison.Ordinal);
        while (indexOf != -1)
        {
            var columnName = template.Substring(indexOf + 1, template.IndexOf("}", indexOf, StringComparison.Ordinal) - indexOf - 1);
            template = template.Replace("{" + columnName + "}", entity.GetCell(new ColumnFullName(null, columnName))!.ToString());
            indexOf = template.IndexOf("{", indexOf + 1, StringComparison.Ordinal);
        }
        return template;
    }

    public static string FillTemplate(this DatabaseKey key, string template)
    {
        int indexOf = 0;
        indexOf = template.IndexOf("{", indexOf, StringComparison.Ordinal);
        while (indexOf != -1)
        {
            var keyIndex = template.Substring(indexOf + 1, template.IndexOf("}", indexOf, StringComparison.Ordinal) - indexOf - 1);
            if (int.TryParse(keyIndex, out var keyIndexNum))
                template = template.Replace("{" + keyIndex + "}", key[keyIndexNum].ToString());
            indexOf = template.IndexOf("{", indexOf + 1, StringComparison.Ordinal);
        }
        return template;
    }
}