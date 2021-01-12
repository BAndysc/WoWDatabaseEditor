using System;
using WDE.Module.Attributes;

namespace WDE.Common.Parameters
{
    [UniqueProvider]
    public interface IParameterFactory
    {
        Parameter Factory(string type, string name);
        Parameter Factory(string type, string name, int defaultValue);

        void Register(string key, Func<string, Parameter> creator);
    }
}