using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.Common.Parameters;
using WDE.Parameters.Models;

namespace WDE.Parameters
{
    [AutoRegister, SingleInstance]
    public class ParameterFactory : IParameterFactory
    {
        private readonly Dictionary<string, ParameterSpecModel> _data = new Dictionary<string, ParameterSpecModel>();
        private readonly Dictionary<string, Func<string, Parameter> > _dynamics = new Dictionary<string, Func<string, Parameter>>();

        public Parameter Factory(string type, string name)
        {
            return Factory(type, name, 0);
        }

        public Parameter Factory(string type, string name, int defaultValue)
        {
            Parameter param;

            if (_dynamics.ContainsKey(type))
                param = _dynamics[type](name);
            else if (_data.ContainsKey(type))
            {
                param = _data[type].IsFlag && _data[type].Values != null ? new FlagParameter(name) : new Parameter(name);

                if (_data[type].Values != null)
                    param.Items = _data[type].Values;
                return param;
            }
            else
                param = new Parameter(name);

            param.SetValue(defaultValue);

            return param;
        }
        
        public void Add(string key, ParameterSpecModel model)
        {
            model.Key = key;
            _data.Add(key, model);
        }

        public IEnumerable<string> GetKeys()
        {
            return _data.Keys.Union(_dynamics.Keys);
        }
        
        public ParameterSpecModel GetDefinition(string key)
        {
            if (_dynamics.ContainsKey(key))
            {
                var param = _dynamics[key](key);
                return new ParameterSpecModel()
                {
                    IsFlag = param is FlagParameter,
                    Key = key,
                    Name = key,
                    Values = param.Items
                };
            }
            return _data[key];
        }

        public void Register(string key, Func<string, Parameter> creator)
        {
            _dynamics.Add(key, creator);
        }
    }

}
