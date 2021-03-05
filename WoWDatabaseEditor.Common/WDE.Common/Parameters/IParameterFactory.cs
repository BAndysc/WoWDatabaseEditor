using System;
using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.Common.Parameters
{
    [UniqueProvider]
    public interface IParameterFactory
    {
        IParameter<long> Factory(string type);
        bool IsRegistered(string type);
        void Register(string key, IParameter<long> parameter);
        
        IEnumerable<string> GetKeys();
    }
}