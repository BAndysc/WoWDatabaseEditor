using System.Collections.Generic;
using System.Linq;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.Parameters.Models;

namespace WDE.Parameters
{
    [AutoRegister]
    [SingleInstance]
    public class ParameterFactory : IParameterFactory
    {
        private readonly Dictionary<string, ParameterSpecModel> data = new();
        private readonly Dictionary<string, IParameter<long>> parameters = new();
        private readonly Dictionary<string, IParameter<string>> stringParameters = new();

        public IParameter<long> Factory(string type)
        {
            if (parameters.TryGetValue(type, out var parameter))
                return parameter;
            return Parameter.Instance;
        }

        public IParameter<string> FactoryString(string type)
        {
            if (stringParameters.TryGetValue(type, out var parameter))
                return parameter;
            return StringParameter.Instance;
        }

        public bool IsRegisteredLong(string type)
        {
            return parameters.ContainsKey(type);
        }

        public bool IsRegisteredString(string type)
        {
            return stringParameters.ContainsKey(type);
        }

        public void Register(string key, IParameter<long> parameter)
        {
            parameters.Add(key, parameter);
        }

        public void Register(string key, IParameter<string> parameter)
        {
            stringParameters.Add(key, parameter);
        }

        public IEnumerable<string> GetKeys() => data.Keys.Union(parameters.Keys);

        public ParameterSpecModel GetDefinition(string key)
        {
            if (parameters.TryGetValue(key, out var param))
            {
                return new ParameterSpecModel
                {
                    IsFlag = param is FlagParameter,
                    Key = key,
                    Name = key,
                    Values = param.Items
                };
            }

            return data[key];
        }
    }
}