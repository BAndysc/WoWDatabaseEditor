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
            switch (definition.ValueType)
            {
                case "string":
                    field = new DbTableField<string>(in definition, (dbValue is string s) ? s : default);
                    break;
                case "float":
                    field = new DbTableField<float>(in definition, (dbValue is float f) ? f : default);
                    break;
                case "bool":
                    var intValue = (dbValue is int bi) ? bi : default;
                    field = new DbTableField<bool>(in definition, intValue > 0);
                    break;
                default:
                    field = new DbTableField<int>(in definition, (dbValue is int i) ? i : default);
                    break;
            }

            return field;
        }
    }
}