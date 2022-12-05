using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;

namespace WDE.SmartScriptEditor.Parameters;

public class TimedActionListParameter : Parameter, ICustomPickerParameter<long>
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IItemFromListProvider itemFromListProvider;

    public TimedActionListParameter(IDatabaseProvider databaseProvider,
        IItemFromListProvider itemFromListProvider)
    {
        this.databaseProvider = databaseProvider;
        this.itemFromListProvider = itemFromListProvider;
    }
    
    public override string ToString(long key)
    {
        return ToString(key, ToStringOptions.WithNumber);
    }

    public override string ToString(long value, ToStringOptions options)
    {
        var entry = (uint)(value / 100);
        var id = value % 100;
        var name = databaseProvider.GetCreatureTemplate(entry);
        
        if (name == null)
            return value.ToString();

        if (options.withNumber)
            return $"{name.Name} #{id} ({value})";
        else
            return $"{name.Name} #{id}";
    }

    public async Task<(long, bool)> PickValue(long value)
    {
        var values = await databaseProvider.GetSmartScriptEntriesByType(SmartScriptType.TimedActionList);
        var dict = values.ToDictionary(x => (long)x, x => new SelectOption(ToString(x, ToStringOptions.WithoutNumber)));
        var result = await itemFromListProvider.GetItemFromList(dict, false, value, "Pick timed action list");
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }
}