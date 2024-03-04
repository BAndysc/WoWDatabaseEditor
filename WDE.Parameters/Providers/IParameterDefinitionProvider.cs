﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common.CoreVersion;
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

        private int GetOrder(string fileName)
        {
            switch (Path.GetFileNameWithoutExtension(fileName))
            {
                case "parameters":
                    return 0;
                case "spell_parameters":
                    return 1;
                case "spell_parameters2":
                    return 2;
            }

            return 3;
        }
        
        public ParameterDefinitionProvider(IMessageBoxService service, ICurrentCoreVersion currentCoreVersion)
        {
            var allParameters = new Dictionary<string, ParameterSpecModel>();
            
            List<string> files;
            if (OperatingSystem.IsBrowser())
            {
                files = new();//
                Console.WriteLine("Parameters not supported in browser yet.");
                //throw new PlatformNotSupportedException("Parameters not supported in browser yet.");
            }
            else
            {
                files = Directory
                    .GetFiles("Parameters/", "*.json", SearchOption.AllDirectories)
                    .OrderBy(GetOrder).ToList();   
            }

            var currentCore = currentCoreVersion.Current;
            
            foreach (var source in files)
            {
                string data = File.ReadAllText(source);
                try {
                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, ParameterSpecModel>>(data);
                    foreach (var keyPair in parameters!) // try-catch is null
                    {
                        if (keyPair.Value.Tags == null || keyPair.Value.Tags.Contains(currentCore.Tag))
                        {
                            allParameters[keyPair.Key] = keyPair.Value;
                        }
                    }
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