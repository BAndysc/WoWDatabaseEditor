using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.TableData;

[UniqueProvider]
public interface ITabularDataPicker
{
    Task<T?> PickRow<T>(ITabularDataArgs<T> args, int defaultSelection = -1) where T : class;
    Task<IReadOnlyCollection<T>> PickRows<T>(ITabularDataArgs<T> args, IReadOnlyList<int>? defaultSelection = null)  where T : class;
}