using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Parameters;

[UniqueProvider]
public interface IParameterPickerService
{
    public Task<(T? value, bool ok)> PickParameter<T>(IParameter<T> parameter, T currentValue, object? context = null) where T : notnull;
    public Task<IReadOnlyCollection<T>> PickMultiple<T>(IParameter<T> parameter) where T : notnull;
}