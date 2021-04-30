using System;
using WDE.Common.Parameters;
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
        
        public IDatabaseField CreateField(string columnName, IValueHolder valueHolder)
        {
            return valueHolder switch
            {
                ValueHolder<string> stringHolder => new DatabaseField<string>(columnName, stringHolder),
                ValueHolder<long> longHolder => new DatabaseField<long>(columnName, longHolder),
                ValueHolder<float> floatHolder => new DatabaseField<float>(columnName, floatHolder),
                _ => throw new Exception("unexpected type: " + valueHolder.GetType())
            };
        }
    }
}