using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Kernel;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Parameters;
using WDE.Common.QuickAccess;
using WDE.MVVM;
using WoWDatabaseEditorCore.Extensions;

namespace WoWDatabaseEditorCore.ViewModels;

public class QuickGoToViewModel : ObservableBase
{
    private IQuickGoToProvider? selectedProvider;
    private QuickGoToItemViewModel? selectedItem;
    private IParameter<long>? parameter;
    
    public QuickGoToViewModel(IEnumerable<IQuickGoToProvider> providers,
        IParameterFactory parameterFactory,
        IEventAggregator eventAggregator)
    {
        Items = new ObservableCollection<QuickGoToItemViewModel>();
        Providers = new ObservableCollection<IQuickGoToProvider>(providers.OrderBy(p => p.Order));
        
        FindItemAsyncPopulator = (_, s, token) =>
        {
            return Task.Run(() =>
            {
                List<object> result = new();
                if (int.TryParse(s, out var id))
                {
                    foreach (var item in Items)
                    {
                        if (token.IsCancellationRequested)
                            return null!;
                        if (item.Key == id)
                            result.Add(item);
                    }
                }
                else
                {
                    foreach (var item in Items)
                    {
                        if (token.IsCancellationRequested)
                            return null!;
                        if (item.Name.Contains(s, StringComparison.InvariantCultureIgnoreCase))
                            result.Add(item);
                    }
                }
                return (IEnumerable)result;
            }, token);
        };
        
        selectedProvider = Providers.FirstOrDefault();
        On(() => SelectedProvider, _ =>
        {
            SelectedItem = null;
            
            if (selectedProvider != null)
            {
                parameter = parameterFactory.Factory(selectedProvider.ParameterKey);
                
                Items.Clear();
                if (parameter.Items != null)
                {
                    Items.AddRange(parameter.Items.Select(x => new QuickGoToItemViewModel(x.Key, x.Value.Name)));
                }
            }
        });
        
        On(() => SelectedItem, _ =>
        {
            if (selectedItem != null && selectedProvider != null)
            {
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(selectedProvider.Create(selectedItem.Key));
            }
        });

        parameterFactory.OnRegister().Subscribe(param =>
        {
            if (selectedProvider != null)
            {
                parameter = parameterFactory.Factory(selectedProvider.ParameterKey);

                if (param == parameter && parameter.Items != null)
                {
                    Items.Clear();
                    Items.AddRange(parameter.Items.Select(x => new QuickGoToItemViewModel(x.Key, x.Value.Name)));
                }
            }
        });
    }
    
    public ObservableCollection<QuickGoToItemViewModel> Items { get; }
    
    public ObservableCollection<IQuickGoToProvider> Providers { get; }
   
    public Func<IEnumerable, string, CancellationToken, Task<IEnumerable>> FindItemAsyncPopulator { get; }

    public QuickGoToItemViewModel? SelectedItem
    {
        get => selectedItem;
        set => SetProperty(ref selectedItem, value);
    }
    
    public IQuickGoToProvider? SelectedProvider
    {
        get => selectedProvider;
        set => SetProperty(ref selectedProvider, value);
    }

    public bool HasAnyType => Providers.Count > 0;
}

public class QuickGoToItemViewModel
{
    public long Key { get; }
    public string Name { get; }

    public QuickGoToItemViewModel(long key, string name)
    {
        Key = key;
        Name = name;
    }
}