using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.QuickAccess;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Parameters.QuickAccess;

[AutoRegister]
public class SpecificParameterSearch : IQuickAccessCommand
{
    private readonly IParameterFactory parameterFactory;
    private readonly IQuickCommands quickCommands;
    private readonly IQuickAccessRegisteredParameters parameters;

    internal SpecificParameterSearch(IParameterFactory parameterFactory, IQuickCommands quickCommands,
        IQuickAccessRegisteredParameters parameters)
    {
        this.parameterFactory = parameterFactory;
        this.quickCommands = quickCommands;
        this.parameters = parameters;
    }
    
    public string Command => "parameter";
    public string? Help => "/parameter <name> <value> - searches for a given <value> in <parameter>";
    
    public async Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        int spaceIndex = text.IndexOf(' ');
        var parameterName = spaceIndex == -1 ? text : text.Substring(0, spaceIndex);
        var parameterValue = spaceIndex == -1 ? "" : text.Substring(spaceIndex + 1);

        if (parameterFactory.IsRegisteredLong(parameterName) || parameterFactory.IsRegisteredLong(parameterName + "Parameter"))
        {
            var parameter = parameterFactory.IsRegisteredLong(parameterName) ? parameterFactory.Factory(parameterName) : parameterFactory.Factory(parameterName + "Parameter");
            
            if (parameter.Items == null)
                return;
            
            if (long.TryParse(parameterValue, out var parameterLong))
            {
                if (parameter is FlagParameter)
                {
                    foreach (var option in parameter.Items)
                    {
                        if ((parameterLong & option.Key) != 0)
                        {
                            produce(new QuickAccessItem(ImageUri.Empty, option.Value.Name, option.Key.ToString(), option.Value.Description ?? "", quickCommands.CopyCommand, option.Key, 70));
                        }
                    }
                }
                else
                {
                    if (parameter.Items.TryGetValue(parameterLong, out var result))
                    {
                        produce(new QuickAccessItem(ImageUri.Empty, result.Name, "(copy)", parameterLong.ToString(), quickCommands.CopyCommand, result.Name, 70));
                    }
                }
            }
            else
            {
                int total = 0;
                foreach (var i in parameter.Items)
                {
                    if (i.Value.Name.Contains(parameterValue, StringComparison.OrdinalIgnoreCase))
                    {
                        total++;
                        produce(new QuickAccessItem(ImageUri.Empty, i.Value.Name, i.Key.ToString(), i.Value.Description ?? "", quickCommands.CopyCommand, i.Key, 70));

                        if (total > 100)
                        {
                            produce(quickCommands.AndMoreItem);
                            return;
                        }
                    }
                }
            }
        }
        else
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                foreach (var parameterKey in parameters.RegisteredAll)
                {
                    var keyName = parameterKey.key.Replace("Parameter", "");
                    var fullCommand = "/parameter " + keyName + " ";
                    produce(new QuickAccessItem(ImageUri.Empty, keyName, "", "", quickCommands.SetSearchCommand, fullCommand, 70));
                }
            }
            else
            {
                var sorted = FuzzySharp.Process.ExtractSorted(parameterName, parameters.RegisteredAll.Select(p => p.key), f => f.ToLower(), null, 70);
                foreach (var proposal in sorted)
                {
                    var keyName = proposal.Value.Replace("Parameter", "");
                    var fullCommand = "/parameter " + keyName + " ";
                    produce(new QuickAccessItem(ImageUri.Empty, keyName, "", "", quickCommands.SetSearchCommand, fullCommand, 70));
                }
            }
        }
    }
}