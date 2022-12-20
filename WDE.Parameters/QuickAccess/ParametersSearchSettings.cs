using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Parameters.QuickAccess;

[UniqueProvider]
internal interface IParametersSearchSettings
{
    INumberSolutionItemProvider? GetProviderForParameter(string parameter);
    void SetProvider(string parameter, INumberSolutionItemProvider? provider);
    void Save();
}

[AutoRegister]
[SingleInstance]
internal class ParametersSearchSettings : IParametersSearchSettings
{
    private readonly IUserSettings userSettings;
    private readonly ISolutionItemProvideService solutionItemProvideService;
    private Dictionary<string, string> parameterToProviderName = new Dictionary<string, string>();
    private Dictionary<string, INumberSolutionItemProvider> nameToProvider = new Dictionary<string, INumberSolutionItemProvider>();

    public ParametersSearchSettings(IUserSettings userSettings,
        ISolutionItemProvideService solutionItemProvideService)
    {
        this.userSettings = userSettings;
        this.solutionItemProvideService = solutionItemProvideService;
        foreach (var provider in solutionItemProvideService.AllCompatible)
        {
            if (provider is INumberSolutionItemProvider numberSolutionItemProvider)
            {
                nameToProvider[numberSolutionItemProvider.GetName()] = numberSolutionItemProvider;
            }
        }
        
        var saved = userSettings.Get<Data>();
        if (saved.actions != null)
        {
            foreach (var action in saved.actions)
            {
                parameterToProviderName[action.parameter] = action.action;
            }
        }
    }
    
    public INumberSolutionItemProvider? GetProviderForParameter(string parameter)
    {
        if (parameterToProviderName.TryGetValue(parameter, out var providerName))
        {
            if (nameToProvider.TryGetValue(providerName, out var provider))
                return provider;
        }

        return null;
    }

    public void SetProvider(string parameter, INumberSolutionItemProvider? provider)
    {
        if (provider == null)
            parameterToProviderName.Remove(parameter);
        else
            parameterToProviderName[parameter] = provider.GetName();
    }

    public void Save()
    {
        var data = new Data();
        data.actions = new List<(string parameter, string action)>();
        foreach (var (parameter, providerName) in parameterToProviderName)
        {
            data.actions.Add((parameter, providerName));
        }
        userSettings.Update(data);
    }

    public struct Data : ISettings
    {
        public List<(string parameter, string action)>? actions;
    }
}