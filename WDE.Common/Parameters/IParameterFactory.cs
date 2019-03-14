using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
