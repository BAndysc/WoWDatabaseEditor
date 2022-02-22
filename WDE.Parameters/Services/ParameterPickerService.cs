using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Module.Attributes;

namespace WDE.Parameters.Services;

[AutoRegister]
[SingleInstance]
public class ParameterPickerService : IParameterPickerService
{
    private readonly IItemFromListProvider itemFromListProvider;

    public ParameterPickerService(IItemFromListProvider itemFromListProvider)
    {
        this.itemFromListProvider = itemFromListProvider;
    }
    
    public async Task<(T?, bool)> PickParameter<T>(IParameter<T> parameter, T currentValue, object? context = null) where T : notnull
    {
        if (!parameter.HasItems)
            return (default, false);

        if (parameter is ICustomPickerContextualParameter<long> custom && context != null)
        {
            var val = await custom.PickValue((currentValue as long?) ?? 0, context);
            if (val.Item2)
                return ((T)(object)val.Item1, true);
            return (default, false);
        }
        
        if (parameter is IParameter<long> longParameter && typeof(T) == typeof(long))
        {
            long? val = await itemFromListProvider.GetItemFromList(longParameter.Items, longParameter is FlagParameter, currentValue as long?);
            if (val.HasValue)
                return ((T)(object)val.Value, true);
        }

        return (default, false);
    }
}