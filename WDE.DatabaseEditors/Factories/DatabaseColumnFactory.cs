using System;
using System.Collections.Generic;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Factories
{
    [SingleInstance]
    [AutoRegister]
    public class DatabaseColumnFactory : IDatabaseColumnFactory
    {
        public IDatabaseColumn CreateColumn(in DbEditorTableGroupFieldJson definition, object? defaultValue)
        {
            IDatabaseColumn column;
            if (definition.ValueType.Contains("Parameter"))
            {
                column = new DatabaseColumn<long>(in definition, new List<IDatabaseField>(), defaultValue ?? 0L);
                return column;
            }

            switch (definition.ValueType)
            {
                case "string":
                    column = new DatabaseColumn<string>(in definition, new List<IDatabaseField>(), defaultValue ?? "");
                    break;
                case "float":
                    column = new DatabaseColumn<float>(in definition, new List<IDatabaseField>(), defaultValue ?? 0.0f);
                    break;
                case "bool":
                case "uint":
                case "int":
                    column = new DatabaseColumn<long>(in definition, new List<IDatabaseField>(), defaultValue ?? 0L);
                    break;
                default:
                    throw new Exception($"Invalid type name for column {definition.DbColumnName}");
            }

            return column;
        }
    }
}