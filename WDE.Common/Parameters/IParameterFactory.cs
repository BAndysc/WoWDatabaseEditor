using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Parameters
{
    public interface IParameterFactory
    {
        Parameter Factory(string type, string name);
        Parameter Factory(string type, string name, int defaultValue);

        void Register(string key, Func<string, Parameter> creator);
    }
}
