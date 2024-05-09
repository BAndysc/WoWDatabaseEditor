using System;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Factories
{
    [AutoRegister]
    [SingleInstance]
    public class DatabaseFieldFactory : IDatabaseFieldFactory
    {
        private readonly IParameterFactory parameterFactory;
        
        public DatabaseFieldFactory(IParameterFactory parameterFactory)
        {
            this.parameterFactory = parameterFactory;
        }
        
        public IDatabaseField CreateField(ColumnFullName columnName, IValueHolder valueHolder)
        {
            return valueHolder switch
            {
                ValueHolder<string> stringHolder => new DatabaseField<string>(columnName, stringHolder),
                ValueHolder<long> longHolder => new DatabaseField<long>(columnName, longHolder),
                ValueHolder<float> floatHolder => new DatabaseField<float>(columnName, floatHolder),
                _ => throw new Exception("unexpected type: " + valueHolder.GetType())
            };
        }

        public IDatabaseField CreateField(DatabaseColumnJson column, object? value)
        {
            IValueHolder valueHolder;
            var type = column.ValueType;

            if (parameterFactory.IsRegisteredLong(type))
                type = "uint";
            else if (parameterFactory.IsRegisteredString(type))
                type = "string";
            else if (type.EndsWith("Parameter"))
                type = "uint";
                
            if (type == "float")
            {
                valueHolder = new ValueHolder<float>(value == null ? 0.0f : Convert.ToSingle(value), column.CanBeNull && value == null);
            }
            else if (type is "int" or "uint" or "long")
            {
                valueHolder = new ValueHolder<long>(value == null ? 0 : Convert.ToInt64(value), column.CanBeNull && value == null);
            }
            else
                valueHolder = new ValueHolder<string>(value is string f ? f : "", column.CanBeNull && value == null);

            return CreateField(column.DbColumnFullName, valueHolder);
        }
    }
}