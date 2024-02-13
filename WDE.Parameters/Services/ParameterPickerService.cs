using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Collections;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.TableData;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.Parameters.ViewModels;

namespace WDE.Parameters.Services;

[AutoRegister]
[SingleInstance]
public class ParameterPickerService : IParameterPickerService
{
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly IParameterFactory parameterFactory;
    private readonly ITabularDataPicker tabularDataPicker;
    private readonly IWindowManager windowManager;

    public ParameterPickerService(IItemFromListProvider itemFromListProvider,
        IParameterFactory parameterFactory,
        ITabularDataPicker tabularDataPicker,
        IWindowManager windowManager)
    {
        this.itemFromListProvider = itemFromListProvider;
        this.parameterFactory = parameterFactory;
        this.tabularDataPicker = tabularDataPicker;
        this.windowManager = windowManager;
    }
    
    public async Task<(T?, bool)> PickParameter<T>(IParameter<T> parameter, T currentValue, object? context = null) where T : notnull
    {
        var (value, ok, hasCustomPicker) = await TryPickUsingCustomParameter(parameter, currentValue, context);
        if (hasCustomPicker)
            return (value, ok);

        if (parameter is IParameter<long> longParameter && typeof(T) == typeof(long))
        {
            long? val = await itemFromListProvider.GetItemFromList(longParameter.Items, longParameter is FlagParameter, currentValue as long?);
            if (val.HasValue)
                return ((T)(object)val.Value, true);
        }

        if (parameter is IParameter<string> stringParameter && typeof(T) == typeof(string))
        {
            string? val = await itemFromListProvider.GetItemFromList(stringParameter.Items, stringParameter is MultiSwitchStringParameter, (currentValue as string) ?? "");
            if (val != null)
                return ((T)(object)val, true);
        }

        return (default, false);
    }

    public async Task<(long value, bool ok)> PickParameter(string parameterName, long currentValue, object? context = null)
    {
        var parameter = parameterFactory.Factory(parameterName);
        return await PickParameter(parameter, currentValue, context);
    }

    private class LongPickerItem
    {
        public LongPickerItem(long key, string name, string? description)
        {
            Key = key;
            Name = name;
            Description = description;
        }

        public long Key { get; init; }
        public string Name { get; init; }
        public string? Description { get; init; }
    }
    
    private class StringPickerItem
    {
        public StringPickerItem(string key, string name, string? description)
        {
            Key = key;
            Name = name;
            Description = description;
        }

        public string Key { get; init; }
        public string Name { get; init; }
        public string? Description { get; init; }
    }
    
    public async Task<IReadOnlyCollection<T>> PickMultiple<T>(IParameter<T> parameter) where T : notnull
    {
        // at this point the parameter system should be overhauled, as it is hard to maintain with the custom pickers :/
        if (parameter is ICustomPickerParameter<string> customString)
        {
            return (await customString.PickMultipleValues()).Select(x => (T)(object)x).ToList();
        }
        
        if (parameter is ICustomPickerParameter<long> customLong)
        {
            return (await customLong.PickMultipleValues()).Select(x => (T)(object)x).ToList();
        }
        
        if (parameter is IParameter<long> longParameter && typeof(T) == typeof(long))
        {
            if (longParameter.Items == null)
            {
                long? val = await itemFromListProvider.GetItemFromList(longParameter.Items, longParameter is FlagParameter);
                if (val.HasValue)
                    return new T[] { (T)(object)val.Value };
                return Array.Empty<T>();
            }

            // this should be unnecessary if one day parameters are overhauled 
            var items = longParameter.Items.Select(item => new LongPickerItem(item.Key, item.Value.Name, item.Value.Description)).ToList();

            var values = await tabularDataPicker.PickRows(new TabularDataBuilder<LongPickerItem>()
                .SetTitle("Pick items")
                .SetData(items.AsIndexedCollection())
                .SetColumns(new TabularDataColumn("Key", "Key", 80),
                    new TabularDataColumn("Name", "Name", 200),
                    new TabularDataColumn("Description", "Description", 300))
                .SetFilter((x, search) =>
                    x.Key.Contains(search) || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .SetExactMatchPredicate((item, search) => item.Key.Is(search))
                .SetExactMatchCreator(search =>
                {
                    if (!long.TryParse(search, out var entry))
                        return null;
                    return new LongPickerItem(entry, "Pick non existing", null);
                })
                .SetNumberPredicate((item, val) => item.Key == val)
                .Build());
            
            return values == null ? Array.Empty<T>() : values.Select(x => (T)(object)x.Key).ToList();
        }

        if (parameter is IParameter<string> stringParameter && typeof(T) == typeof(string))
        {
            if (stringParameter.Items == null)
            {
                string? val = await itemFromListProvider.GetItemFromList(stringParameter.Items, stringParameter is MultiSwitchStringParameter);
                if (val != null)
                    return new T[] { (T)(object)val };
                return Array.Empty<T>();
            }
            
            // this should be unnecessary if one day parameters are overhauled 
            var items = stringParameter.Items.Select(item => new StringPickerItem(item.Key, item.Value.Name, item.Value.Description)).ToList();

            var values = await tabularDataPicker.PickRows(new TabularDataBuilder<StringPickerItem>()
                .SetTitle("Pick items")
                .SetData(items.AsIndexedCollection())
                .SetColumns(new TabularDataColumn("Key", "Key", 120),
                    new TabularDataColumn("Name", "Name", 200),
                    new TabularDataColumn("Description", "Description", 300))
                .SetFilter((x, search) =>
                    x.Key.Contains(search, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .SetExactMatchPredicate((item, search) => item.Key == search)
                .SetExactMatchCreator(search => new StringPickerItem(search, "Pick non existing", null))
                .Build());
            
            return values == null ? Array.Empty<T>() : values.Select(x => (T)(object)x.Key).ToList();
        }

        return Array.Empty<T>();
    }

    public async Task<IReadOnlyList<long>?> PickMultipleSimple(IReadOnlyList<long> current, IParameter<long> parameter)
    {
        using var vm = new MultipleParametersPickerViewModel(this, parameter, current);
        if (await windowManager.ShowDialog(vm))
            return vm.Result;
        return null;
    }

    private async Task<(T?, bool accept, bool hasCustomPicker)> TryPickUsingCustomParameter<T>(IParameter<T> parameter, T? currentValue, object? context = null) where T : notnull
    {
        if (parameter is ICustomPickerContextualParameter<long> custom && context != null)
        {
            var val = await custom.PickValue((currentValue as long?) ?? 0, context);
            if (val.Item2)
                return ((T)(object)val.Item1, true, true);
            return (default, false, true);
        }
        
        if (parameter is ICustomPickerContextualParameter<string> customStringPicker && context != null)
        {
            var val = await customStringPicker.PickValue((currentValue as string) ?? "", context);
            if (val.Item2)
                return ((T)(object)val.Item1, true, true);
            return (default, false, true);
        }
        
        if (parameter is ICustomPickerParameter<string> customString)
        {
            var val = await customString.PickValue((currentValue as string) ?? "");
            if (val.Item2)
                return ((T)(object)val.Item1, true, true);
            return (default, false, true);
        }
        
        if (parameter is ICustomPickerParameter<long> customLong)
        {
            var val = await customLong.PickValue((long?)(object?)(currentValue) ?? 0L);
            if (val.Item2)
                return ((T)(object)val.Item1, true, true);
            return (default, false, true);
        }

        return (default, false, false);
    }
}