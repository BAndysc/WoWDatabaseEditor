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

        public IParameter<long> Factory(string type)
        {
            if (parameters.TryGetValue(type, out var parameter))
                return parameter;
            return Parameter.Instance;
        }

        public bool IsRegistered(string type)
        {
            return parameters.ContainsKey(type);
        }

        public void Register(string key, IParameter<long> parameter)
        {
            parameters.Add(key, parameter);
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