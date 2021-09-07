using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common.Utils;
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

            var files = Directory
                .GetFiles("Parameters/", "*.json", SearchOption.AllDirectories)
                .OrderBy(t => t, Compare.CreateComparer<string>((a, b) =>
                {
                    if (Path.GetFileName(a) == "parameters.json")
                        return -1;
                    return 1;
                })).ToList();
            
            foreach (var source in files)
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