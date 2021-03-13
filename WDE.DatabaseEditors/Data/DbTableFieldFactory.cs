using System;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;
using WDE.Parameters;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.Data
{
    [AutoRegister]
    [SingleInstance]
    public class DbTableFieldFactory : IDbTableFieldFactory
    {
        private readonly IParameterFactory parameterFactory;
        
        public DbTableFieldFactory(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }
        
        public IDbTableField CreateField(in DbEditorTableGroupFieldJson definition, object dbValue)
        {
            IDbTableField field;
            if (definition.ValueType.Contains("Parameter"))
            {
                field = new DbTableField<long>(in definition, CreateParameterValue(in definition, dbValue, definition.DbColumnName));
                return field;
            }

            switch (definition.ValueType)
            {
                case "string":
                    field = new DbTableField<string>(in definition, CreateStringValue(dbValue, definition.DbColumnName));
                    break;
                case "float":
                    field = new DbTableField<float>(in definition, CreateFloatValue(dbValue, definition.DbColumnName));
                    break;
                case "bool":
                    field = new DbTableField<long>(in definition, CreateLongValue(dbValue, definition.DbColumnName, true));
                    break;
                case "uint":
                case "int":
                    field = new DbTableField<long>(in definition, CreateLongValue(dbValue, definition.DbColumnName, false));
                    break;
                default:
                    throw new Exception($"[DbTableFieldFactory::CreateField] Invalid type name for field {definition.DbColumnName}");
            }
            return field;
        }

        private ParameterValueHolder<long> CreateParameterValue(in DbEditorTableGroupFieldJson definition, object dbValue,
            string fieldName)
        {
            var parameter = parameterFactory.Factory(definition.ValueType);
            if (parameter != null)
            {
                try
                {
                    var longVal = Convert.ToInt64(dbValue);
                    var paramHolder = new ParameterValueHolder<long>(parameter);
                    paramHolder.Value = longVal;
                    return paramHolder;
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"Field {fieldName} db value type doesn't match declared type! Expected decimal number type, got {dbValue.GetType()}");
                }
            }

            throw new Exception($"Field {fieldName} has not known parameter type, type is {definition.ValueType} but it doesn't exist!");
        }
        
        private ParameterValueHolder<string> CreateStringValue(object dbValue, string fieldName)
        {
            var parameter = new ParameterValueHolder<string>(new StringParameter());
            if (dbValue is DBNull)
                return parameter;

            var val = Convert.ToString(dbValue);
            if (val != null)
            {
                parameter.Value = val;
                return parameter;
            }
            
            throw new Exception($"Field {fieldName} db value type doesn't match declared type! Expected string, got {dbValue.GetType()}");
        }

        private ParameterValueHolder<float> CreateFloatValue(object dbValue, string fieldName)
        {
            var parameter = new ParameterValueHolder<float>(FloatParameter.Instance);
            try
            {
                var val = Convert.ToSingle(dbValue);
                parameter.Value = val;
                return parameter;
            }
            catch (Exception e)
            {
                throw new Exception($"Field {fieldName} db value type doesn't match declared type! Expected float, got {dbValue.GetType()}");
            }
        }

        private ParameterValueHolder<long> CreateLongValue(object dbValue, string fieldName, bool asBool)
        {
            var parameter = new ParameterValueHolder<long>(asBool ? new BoolParameter() : Parameter.Instance);
            try
            {
                var val = Convert.ToInt64(dbValue);
                parameter.Value = val;
                return parameter;
            }
            catch (Exception e)
            {
                throw new Exception($"Field {fieldName} db value type doesn't match declared type! Expected float, got {dbValue.GetType()}");
            }
        }
    }
}