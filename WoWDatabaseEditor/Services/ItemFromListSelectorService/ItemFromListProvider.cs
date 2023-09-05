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

namespace WoWDatabaseEditorCore.Services.ItemFromListSelectorService
{
    [AutoRegister]
    public class ItemFromListProvider : IItemFromListProvider
    {
        private readonly IWindowManager windowManager;
        private readonly ITabularDataPicker tabularDataPicker;

        public ItemFromListProvider(IWindowManager windowManager,
            ITabularDataPicker tabularDataPicker)
        {
            this.windowManager = windowManager;
            this.tabularDataPicker = tabularDataPicker;
        }
        
        public async Task<long?> GetItemFromList(Dictionary<long, SelectOption>? items, bool flags, long? current = null, string? title = null)
        {
            if (items != null)
            {
                var data = items.ToList();
                var anyHasDescription = data.Any(x => x.Value.Description != null);
                var columns = new List<ITabularDataColumn>()
                {
                    new TabularDataColumn("Key", flags ? "Flag" : "Key", 75),
                    new TabularDataColumn("Value.Name", "Name", 200)
                };
                if (anyHasDescription)
                    columns.Add(new TabularDataColumn("Value.Description", "Description", 400));
                var table = new TabularDataBuilder<KeyValuePair<long, SelectOption>>()
                    .SetTitle(title ?? "Pick")
                    .SetColumns(columns)
                    .SetFilter((obj, search) => obj.Key.Contains(search) || obj.Value.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || (obj.Value.Description != null && obj.Value.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .SetExactMatchPredicate((obj, search) => obj.Key.Is(search) || !long.TryParse(search, out _))
                    .SetExactMatchCreator(text => new KeyValuePair<long, SelectOption>(long.Parse(text), new SelectOption("Pick non existing")))
                    .SetData(data.AsIndexedCollection());

                if (flags)
                {
                    List<int>? checkedIndices = null;
                    if (current.HasValue)
                    {
                        checkedIndices = new List<int>();
                        for (var index = 0; index < data.Count; index++)
                        {
                            var item = data[index];
                            if (item.Key == 0 && current.Value == 0 || current.Value > 0 && (item.Key & current.Value) == item.Key)
                                checkedIndices.Add(index);
                        }
                    }
                    
                    table = table.SetNumberPredicate((element, val) => (element.Key & val) == element.Key || element.Value.Name.Contains(val.ToString()))
                        .SetExactMatchPredicate((obj, search) => obj.Key.Is(search) || !long.TryParse(search, out var flag) || !IsPowerOfTwo(flag));

                    var result = await tabularDataPicker.PickStructRows(table.Build(), checkedIndices, null, true);
                    if (result == null)
                        return null;
                    long newValue = 0;
                    foreach (var checkedItem in result)
                        newValue |= checkedItem.Key;
                    return newValue;
                }
                else
                {
                    int defaultSelection = current.HasValue ? data.IndexIf(x => x.Key == current.Value) : -1;

                    var result = await tabularDataPicker.PickStructRow(table.Build(), defaultSelection, current.HasValue && current.Value > 0 ? current.Value.ToString() : null);
                    if (result.HasValue)
                        return result.Value.Key;
                    return null;   
                }
            }

            using LongItemFromListProviderViewModel vm = new(items, flags, current, title);
            if (current.HasValue && current.Value != 0)
                vm.SearchText = current.Value.ToString();
            if (await windowManager.ShowDialog(vm))
                return vm.GetEntry();
            return null;
        }

        private bool IsPowerOfTwo(long result)
        {
            return (result & (result - 1)) == 0;
        }

        public async Task<string?> GetItemFromList(Dictionary<string, SelectOption>? items, bool multiSelect, string? current = null, string? title = null)
        {
            using StringItemFromListProviderViewModel vm = new(items, multiSelect, current, title);
            if (current != null)
                vm.SearchText = current;
            if (await windowManager.ShowDialog(vm))
                return vm.GetEntry();
            return null;
        }
        
        public async Task<float?> GetItemFromList(Dictionary<float, SelectOption>? items, string? title = null)
        {
            using FloatItemFromListProviderViewModel vm = new(items, 0, title);
            if (await windowManager.ShowDialog(vm))
                return vm.GetEntry();
            return null;
        }
    }
}