using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common.Services.MessageBox;
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

        public ParameterDefinitionProvider(IMessageBoxService service)
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
                try {
                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, ParameterSpecModel>>(data);
                    foreach (var keyPair in parameters)
                        allParameters[keyPair.Key] = keyPair.Value;
                } 
                catch (Exception e)
                {
                    service.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Error while loading parameters")
                        .SetMainInstruction("Parameters file is corrupted")
                        .SetContent("File " + source +
                                    " is corrupted, either this is a faulty update or you have made a faulty change.\n\n"  + e.Message)
                        .WithOkButton(true)
                        .Build()).ListenErrors();
                }
            }

            Parameters = allParameters;
        }
    }

    public interface IParameterDefinitionProvider
    {
        IReadOnlyDictionary<string, ParameterSpecModel> Parameters { get; }
    }
}