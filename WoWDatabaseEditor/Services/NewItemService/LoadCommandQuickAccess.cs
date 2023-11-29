using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.QuickAccess;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.NewItemService;

[AutoRegister]
public class LoadCommandQuickAccess : IQuickAccessCommand, IQuickAccessSearchProvider
{
    private readonly IQuickCommands quickCommands;
    private readonly IEventAggregator eventAggregator;
    private readonly IParameterFactory parameterFactory;
    public string Command => "open";
    public string? Help => "/open <type> - opens a document of given type";

    private List<ISolutionItemProvider> providers = new();
    private Dictionary<string, ISolutionItemProvider> byName;
    private List<string> names = new();

    private ICommand OpenItemCommand;
    private ICommand DirectCreateCommand;
    
    public LoadCommandQuickAccess(ISolutionItemProvideService itemProvideService,
        IQuickCommands quickCommands, Lazy<IQuickAccessViewModel> quickAccessViewModel,
        IEventAggregator eventAggregator, IParameterFactory parameterFactory,
        IMainThread mainThread)
    {
        byName = new(StringComparer.OrdinalIgnoreCase);
        this.quickCommands = quickCommands;
        this.eventAggregator = eventAggregator;
        this.parameterFactory = parameterFactory;
        providers = itemProvideService.AllCompatible.ToList();
        names = providers.Select(p => p.GetName()).ToList();
        foreach (var p in providers)
            byName[p.GetName()] = p;

        OpenItemCommand = new DelegateCommand<ISolutionItemProvider>(prov =>
        {
            quickAccessViewModel.Value.CloseSearch();
            mainThread.Delay(() => CreateItem(prov).ListenErrors(), TimeSpan.FromMilliseconds(1));
        });

        DirectCreateCommand = new DelegateCommand<(INumberSolutionItemProvider, long)?>(pair =>
        {
            if (!pair.HasValue)
                return;
            
            quickAccessViewModel.Value.CloseSearch();
            mainThread.Delay(() => DirectCreateItem(pair.Value.Item1, pair.Value.Item2).ListenErrors(), TimeSpan.FromMilliseconds(1));
        });
    }

    private async Task DirectCreateItem(INumberSolutionItemProvider provider, long entry)
    {
        var item = await provider.CreateSolutionItem(entry);
        if (item != null)
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
    }

    private async Task CreateItem(ISolutionItemProvider provider)
    {
        ISolutionItem? item = await provider.CreateSolutionItem();
        if (item != null)
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
    }
    
    private string GetProviderName(ISolutionItemProvider provider)
    {
        var hasSpace = provider.GetName().IndexOf(' ') >= 0;
        if (hasSpace)
            return '"' + provider.GetName() + '"';
        return provider.GetName();
    }

    private ISolutionItemProvider? GetByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;
        
        name = name.ToLower();
        
        if (byName.TryGetValue(name, out var provider))
            return provider;

        ISolutionItemProvider? bestMatch = null;
        int matching = 0;
        int containing = 0;
        
        foreach (var p in providers)
        {
            if (p.GetName().StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                bestMatch = p;
                matching++;
            }

            if (p.GetName().Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                containing++;
            }
        }

        if (matching == 1 && containing == 1)
            return bestMatch;
        return null;
    }
    
    public Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        Tokenizer tokenizer = new Tokenizer(text);
        var type = tokenizer.Next();
        var value = tokenizer.Remaining();

        var provider = GetByName(type.ToString());
        
        if (type.IsEmpty)
        {
            foreach (var item in providers)
            {
                if (item.IsContainer)
                    continue;
                produce(CreateListItem(item));
            }
        }
        else
        {
            if (provider is INumberSolutionItemProvider longProvider)
            {
                var source = parameterFactory.Factory(longProvider.ParameterName);

                if (source.Items != null)
                {
                    if (long.TryParse(value, out var longValue))
                    {
                        if (source.Items.TryGetValue(longValue, out var item))
                            produce(new QuickAccessItem(longProvider.GetImage(), 
                                item.Name, longValue.ToString(), "", DirectCreateCommand, (longProvider, longValue)));
                        else
                            produce(new QuickAccessItem(longProvider.GetImage(), 
                                "(non existing)", longValue.ToString(), "", DirectCreateCommand, (longProvider, longValue)));
                        return Task.CompletedTask;
                    }
                    else
                    {
                        int total = 0;
                        foreach (var item in source.Items)
                        {
                            if (item.Value.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                            {
                                produce(new QuickAccessItem(longProvider.GetImage(), item.Value.Name, item.Key.ToString(), "", DirectCreateCommand, (longProvider, (long)item.Key)));
                                total++;
                                if (total > 100)
                                {
                                    produce(quickCommands.AndMoreItem);   
                                    break;
                                }
                            }
                        }
                        if (total > 0)
                            return Task.CompletedTask;
                    }
                }
            }

            var sorted = FuzzySharp.Process.ExtractSorted(text, names, p => p.ToLower(), null, 70);
            foreach (var s in sorted)
            {
                var item = providers[s.Index];
                if (item.IsContainer)
                    continue;
                produce(CreateListItem(item));
            }
        }

        return Task.CompletedTask;
    }

    private QuickAccessItem CreateListItem(ISolutionItemProvider item)
    {
        if (item is INumberSolutionItemProvider numberedProvider && 
            numberedProvider.ParameterName != "Parameter" &&
            parameterFactory.IsRegisteredLong(numberedProvider.ParameterName))
        {
            var fullCommand = "/open " + GetProviderName(item) + " ";
            return new QuickAccessItem(item.GetImage(), item.GetName(), "", item.GetGroupName(), quickCommands.SetSearchCommand, fullCommand);
        }
            
        return new QuickAccessItem(item.GetImage(), item.GetName(), "", item.GetGroupName(), OpenItemCommand, item);
    }
}