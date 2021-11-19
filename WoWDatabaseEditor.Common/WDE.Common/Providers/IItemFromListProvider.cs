using System.Collections.Generic;
using WDE.Common.Parameters;
using WDE.Module.Attributes;

namespace WDE.Common.Providers
{
    [UniqueProvider]
    public interface IItemFromListProvider
    {
        System.Threading.Tasks.Task<float?> GetItemFromList(Dictionary<float, SelectOption>? items, string? title = null);
        System.Threading.Tasks.Task<long?> GetItemFromList(Dictionary<long, SelectOption>? items, bool flag, long? current = null, string? title = null);
        System.Threading.Tasks.Task<string?> GetItemFromList(Dictionary<string, SelectOption>? items, bool multiSelect, string? current = null, string? title = null);
    }
}