using System;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;
using WDE.Parameters;
using WDE.Parameters.Models;

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
                return new DatabaseField<string>(stringHolder);
            }
            else if (valueHolder is ValueHolder<long> longHolder)
            {
                return new DatabaseField<long>(longHolder);
            }
            else if (valueHolder is ValueHolder<float> floatHolder)
            {
                return new DatabaseField<float>(floatHolder);
            }

            throw new Exception("unexpected type: " + valueHolder.GetType());
        }
    }
}