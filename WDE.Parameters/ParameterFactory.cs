using System;
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
        private readonly Dictionary<string, Func<string, Parameter>> dynamics = new();

        public Parameter Factory(string type, string name)
        {
            return Factory(type, name, 0);
        }

        public Parameter Factory(string type, string name, int defaultValue)
        {
            Parameter param;

            if (dynamics.ContainsKey(type))
                param = dynamics[type](name);
            else if (data.ContainsKey(type))
            {
                param = data[type].IsFlag && data[type].Values != null ? new FlagParameter(name) : new Parameter(name);

                if (data[type].Values != null)
                    param.Items = data[type].Values;
                return param;
            }
            else
                param = new Parameter(name);

            param.SetValue(defaultValue);

            return param;
        }

        public void Register(string key, Func<string, Parameter> creator)
        {
            dynamics.Add(key, creator);
        }

        public void Add(string key, ParameterSpecModel model)
        {
            model.Key = key;
            data.Add(key, model);
        }

        public IEnumerable<string> GetKeys()
        {
            return data.Keys.Union(dynamics.Keys);
        }

        public ParameterSpecModel GetDefinition(string key)
        {
            if (dynamics.ContainsKey(key))
            {
                Parameter param = dynamics[key](key);
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