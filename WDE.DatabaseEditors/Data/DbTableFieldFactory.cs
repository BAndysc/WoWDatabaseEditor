using System;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    [SingleInstance]
    public class DbTableFieldFactory : IDbTableFieldFactory
    {
        public IDbTableField CreateField(in DbEditorTableGroupFieldJson definition, object dbValue)
        {
            IDbTableField field;
            if (definition.ValueType.Contains("Parameter"))
            {
                field = new DbTableField<long>(in definition, ConvertValue<uint>(dbValue, definition.DbColumnName));
                return field;
            }

            switch (definition.ValueType)
            {
                case "string":
                    field = new DbTableField<string>(in definition, ConvertValue<string>(dbValue, definition.DbColumnName));
                    break;
                case "float":
                    field = new DbTableField<float>(in definition, ConvertValue<float>(dbValue, definition.DbColumnName));
                    break;
                case "bool":
                    var intValue = ConvertValue<uint>(dbValue, definition.DbColumnName);
                    field = new DbTableField<bool>(in definition, intValue > 0);
                    break;
                case "uint":
                    field = new DbTableField<uint>(in definition, ConvertValue<uint>(dbValue, definition.DbColumnName));
                    break;
                case "int":
                    field = new DbTableField<long>(in definition, ConvertValue<int>(dbValue, definition.DbColumnName));
                    break;
                default:
                    throw new Exception($"[DbTableFieldFactory::CreateField] Invalid type name for field {definition.DbColumnName}");
            }
            return field;
        }

        private static T ConvertValue<T>(object dbValue, string fieldName)
        {
            // allow null return when value is null and type is string
            if (dbValue is DBNull && typeof(T) == typeof(string))
                return default;
            if (dbValue is T fieldValue)
                return fieldValue;

            //
            // if (typeof(T) == typeof(int) && dbValue != null)
                // return dbValue as T;
            return default;
                // throw new Exception($"Field {fieldName} db value doesn't match declared type! Expecting {typeof(T).Name} got {dbValue.GetType().Name}");
        }
    }
}