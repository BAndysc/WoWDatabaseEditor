using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.Parameters.QuickAccess;

[AutoRegister]
internal class ParameterSearchConfiguration : ObservableBase, IConfigurable
{
    public ParameterSearchConfiguration(ISolutionItemProvideService solutionItemProvideService,
        IParametersSearchSettings settings,
        IQuickAccessRegisteredParameters parameters)
    {
        Dictionary<string, List<INumberSolutionItemProvider>> byParameterProviders = new();
        foreach (var provider in solutionItemProvideService.AllCompatible)
        {
            if (provider is INumberSolutionItemProvider numberSolutionItemProvider)
            {
                var parameter = numberSolutionItemProvider.ParameterName;
                if (!byParameterProviders.TryGetValue(parameter, out var list))
                    list = byParameterProviders[parameter] = new();
                list.Add(numberSolutionItemProvider);
            }
        }

        foreach (var param in parameters.RegisteredAll)
        {
            if (byParameterProviders.TryGetValue(param.key, out var providers))
            {
                var parameter = new SearchConfigurationParameter(settings, param.name, param.key, providers);
                parameter.ToObservable(o => o.IsModified).SubscribeAction(_ => RaisePropertyChanged(nameof(IsModified)));
                Parameters.Add(parameter);
            }
        }

        Save = new DelegateCommand(() =>
        {
            foreach (var param in Parameters)
            {
                settings.SetProvider(param.ParameterKey, param.Action.Provider);
                param.Save();
            }
            settings.Save();
        });
    }
    
    public ICommand Save { get; }
    public string Name => "Quick access";
    public string? ShortDescription => "You can configure the default action in quick access window for each parameter here";
    public bool IsModified => Parameters.Any(p => p.IsModified);
    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
    public List<SearchConfigurationParameter> Parameters { get; } = new();
}

internal partial class SearchConfigurationParameter
{
    public SearchConfigurationParameter(IParametersSearchSettings settings, 
        string friendlyName, 
        string parameterKey, 
        List<INumberSolutionItemProvider> providers)
    {
        ParameterName = friendlyName;
        ParameterKey = parameterKey;
        Actions = providers.Select(p => new SearchConfigurationAction(p, p.GetName(), p.GetName())).ToList();
        Actions.Add(new SearchConfigurationAction(null, "Copy", "copy"));
        if (settings.GetProviderForParameter(parameterKey) is { } provider)
        {
            var indexOf = providers.IndexOf(provider);
            if (indexOf == -1)
                action = savedAction = Actions[^1];
            else
                action = savedAction = Actions[indexOf];
        }
        else
            action = savedAction = Actions[^1];
    }

    public void Save() => SavedAction = action;

    public bool IsModified => action != savedAction;
    public string ParameterName { get; }
    public string ParameterKey { get; }
    public List<SearchConfigurationAction> Actions { get; }
    [AlsoNotify(nameof(IsModified))] [Notify] private SearchConfigurationAction action;
    [AlsoNotify(nameof(IsModified))] [Notify] private SearchConfigurationAction savedAction;
}

internal class SearchConfigurationAction
{
    public INumberSolutionItemProvider? Provider { get; }
    public string Name { get; }
    public string Key { get; }
    
    public SearchConfigurationAction(INumberSolutionItemProvider? provider, string name, string key)
    {
        Provider = provider;
        Name = name;
        Key = key;
    }
}