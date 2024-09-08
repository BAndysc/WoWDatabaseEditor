using System.Collections.Generic;
using System.Linq;
using WDE.Common;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Parameters.QuickAccess;

[UniqueProvider]
internal interface IParametersSearchSettings
{
    INumberSolutionItemProvider? GetProviderForParameter(string parameter);
    INumberSolutionItemProvider? GetDefaultProviderForParameter(string parameter);
    INumberSolutionItemProvider? GetSavedProviderForParameter(string parameter);
    bool IsCopyForParameter(string parameter);
    void SetProvider(string parameter, INumberSolutionItemProvider provider);
    void SetCopy(string parameter);
    void ResetProvider(string parameter);
    void Save();
}

[AutoRegister]
[SingleInstance]
internal class ParametersSearchSettings : IParametersSearchSettings
{
    private readonly IUserSettings userSettings;
    private readonly ISolutionItemProvideService solutionItemProvideService;
    private Dictionary<string, string> parameterToProviderName = new Dictionary<string, string>();
    private HashSet<string> parametersAsCopy = new();
    private Dictionary<string, INumberSolutionItemProvider> nameToProvider = new Dictionary<string, INumberSolutionItemProvider>();
    private Dictionary<string, List<INumberSolutionItemProvider>> parameterNameToProviders = new();

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
                if (numberSolutionItemProvider.ParameterName != null)
                {
                    if (!parameterNameToProviders.TryGetValue(numberSolutionItemProvider.ParameterName, out var list))
                        list = parameterNameToProviders[numberSolutionItemProvider.ParameterName] = new();
                    list.Add(numberSolutionItemProvider);
                }
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

        if (saved.copy != null)
        {
            parametersAsCopy.Clear();
            foreach (var param in saved.copy)
                parametersAsCopy.Add(param);
        }
    }
    
    public INumberSolutionItemProvider? GetProviderForParameter(string parameter)
    {
        if (IsCopyForParameter(parameter))
            return null;
        if (GetSavedProviderForParameter(parameter) is { } provider)
            return provider;
        if (GetDefaultProviderForParameter(parameter) is { } provider2)
            return provider2;

        return null;
    }

    public INumberSolutionItemProvider? GetDefaultProviderForParameter(string parameter)
    {
        return parameterNameToProviders.TryGetValue(parameter, out var list) ? list.FirstOrDefault() : null;
    }

    public INumberSolutionItemProvider? GetSavedProviderForParameter(string parameter)
    {
        if (parameterToProviderName.TryGetValue(parameter, out var providerName))
        {
            if (nameToProvider.TryGetValue(providerName, out var provider))
                return provider;
        }

        return null;
    }

    public bool IsCopyForParameter(string parameter) => parametersAsCopy.Contains(parameter);

    public void SetProvider(string parameter, INumberSolutionItemProvider provider)
    {
        parameterToProviderName[parameter] = provider.GetName();
        parametersAsCopy.Remove(parameter);
    }

    public void SetCopy(string parameter)
    {
        parametersAsCopy.Add(parameter);
        parameterToProviderName.Remove(parameter);
    }

    public void ResetProvider(string parameter)
    {
        parameterToProviderName.Remove(parameter);
        parametersAsCopy.Remove(parameter);
    }

    public void Save()
    {
        var data = new Data();
        data.actions = new List<(string parameter, string action)>();
        data.copy = new(parametersAsCopy);
        foreach (var (parameter, providerName) in parameterToProviderName)
        {
            data.actions.Add((parameter, providerName));
        }
        userSettings.Update(data);
    }

    public struct Data : ISettings
    {
        public List<(string parameter, string action)>? actions;
        public List<string>? copy;
    }
}