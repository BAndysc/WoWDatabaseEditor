using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.TableData;

[UniqueProvider]
public interface ITabularDataPicker
{
    Task<T?> PickRow<T>(ITabularDataArgs<T> args);
}