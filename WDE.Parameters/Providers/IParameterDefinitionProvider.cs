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
            var allParameters = new Dictionary<string, ParameterSpecModel>();

            foreach (var source in Directory.GetFiles("Parameters/", "*.json", SearchOption.AllDirectories))
            {
                string data = File.ReadAllText(source);
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, ParameterSpecModel>>(data);
                foreach (var keyPair in parameters)
                    allParameters[keyPair.Key] = keyPair.Value;
            }

            Parameters = allParameters;
        }
    }

    public interface IParameterDefinitionProvider
    {
        IReadOnlyDictionary<string, ParameterSpecModel> Parameters { get; }
    }
}