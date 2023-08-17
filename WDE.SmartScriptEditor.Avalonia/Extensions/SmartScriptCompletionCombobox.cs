using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuzzySharp;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.Avalonia.Extensions;

public static class SmartScriptCompletionCombobox
{
    public static Func<IEnumerable, string, CancellationToken, Task<IEnumerable>> AsyncPopulator
    {
        get
        {
            return async (items, s, token) =>
            {
                if (items is not IList o)
                    return Enumerable.Empty<object>();

                if (string.IsNullOrEmpty(s))
                    return items;

                return await Task.Run(() =>
                {
                    bool isNumberSearch = long.TryParse(s, out var number);
                    if (o.Count < 250)
                    {
                        int exactMatchIndex = -1;
                        var result = Process.ExtractSorted(s, items.Cast<object>().Select(item => item.ToString()), cutoff: 51)
                            .Select((item, index) =>
                            {
                                var option = (ParameterOption)o[item.Index]!;
                                if (isNumberSearch && option.Value == number)
                                    exactMatchIndex = index;
                                return o[item.Index]!;
                            }).ToList();
                        if (exactMatchIndex != -1)
                        {
                            var exactMatch = (ParameterOption)result[exactMatchIndex]!;
                            result.RemoveAt(exactMatchIndex);
                            result.Insert(0, exactMatch);
                        }
                        return result;
                    }

                    List<object> picked = new();
                    var search = s.ToLower();
                    foreach (ParameterOption item in o)
                    {
                        if (isNumberSearch && item.Value == number)
                            picked.Insert(0, item);
                        else if (item.ToString()!.Contains(search, StringComparison.InvariantCultureIgnoreCase))
                            picked.Add(item);

                        if (token.IsCancellationRequested)
                            break;
                    }

                    return picked;
                }, token);
            };
        }
    }
}