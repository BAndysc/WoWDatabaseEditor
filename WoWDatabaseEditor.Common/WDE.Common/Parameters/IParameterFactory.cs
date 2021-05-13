using System;
using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Parameters
{
    [UniqueProvider]
    public interface IParameterFactory
    {
        IParameter<long> Factory(string type);
        IParameter<string> FactoryString(string type);
        bool IsRegisteredLong(string type);
        bool IsRegisteredString(string type);
        void Register(string key, IParameter<long> parameter);
        void Register(string key, IParameter<string> parameter);
        
        IEnumerable<string> GetKeys();
    }
}