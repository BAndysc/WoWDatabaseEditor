using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WDE.Module.Attributes;
using WDE.Parameters.Models;

namespace WDE.Parameters.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class ParameterDefinitionProvider : IParameterDefinitionProvider
    {
        public IReadOnlyDictionary<string, ParameterSpecModel> Parameters { get; }

        public ParameterDefinitionProvider()
        {
            var source = "Data/parameters.json";
            if (File.Exists(source))
            {
                string data = File.ReadAllText(source);
                Parameters = JsonConvert.DeserializeObject<Dictionary<string, ParameterSpecModel>>(data);
            }
            else
                Parameters = new Dictionary<string, ParameterSpecModel>();
        }
    }

    public interface IParameterDefinitionProvider
    {
        IReadOnlyDictionary<string, ParameterSpecModel> Parameters { get; }
    }
}