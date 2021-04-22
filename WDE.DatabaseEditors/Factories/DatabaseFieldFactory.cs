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
            if (valueHolder is ValueHolder<string> stringHolder)
            {
                return new DatabaseField<string>(columnName, stringHolder);
            }
            else if (valueHolder is ValueHolder<long> longHolder)
            {
                return new DatabaseField<long>(columnName, longHolder);
            }
            else if (valueHolder is ValueHolder<float> floatHolder)
            {
                return new DatabaseField<float>(columnName, floatHolder);
            }

            throw new Exception("unexpected type: " + valueHolder.GetType());
        }
    }
}