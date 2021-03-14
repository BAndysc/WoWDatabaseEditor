using System;
using System.Collections.Generic;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [SingleInstance]
    [AutoRegister]
    public class DbTableColumnFactory : IDbTableColumnFactory
    {
        public IDbTableColumn CreateColumn(in DbEditorTableGroupFieldJson definition)
        {
            IDbTableColumn column;
            if (definition.ValueType.Contains("Parameter"))
            {
                column = new DbTableColumn<long>(in definition, new List<IDbTableField>());
                return column;
            }

            switch (definition.ValueType)
            {
                case "string":
                    column = new DbTableColumn<string>(in definition, new List<IDbTableField>());
                    break;
                case "float":
                    column = new DbTableColumn<float>(in definition, new List<IDbTableField>());
                    break;
                case "bool":
                case "uint":
                case "int":
                    column = new DbTableColumn<long>(in definition, new List<IDbTableField>());
                    break;
                default:
                    throw new Exception($"Invalid type name for column {definition.DbColumnName}");
            }

            return column;
        }
    }
}