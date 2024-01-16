using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.TableData;
using WDE.Conditions.Shared;
using WDE.Parameters.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class SmartQuestObjectiveParameter : BaseQuestObjectiveParameter,
    IAsyncContextualParameter<long, SmartBaseElement>, 
    IAsyncContextualParameter<long, IConditionViewModel>,
    IAffectedByOtherParametersParameter
{ 
    public SmartQuestObjectiveParameter(ICachedDatabaseProvider databaseProvider, 
        IItemFromListProvider itemFromListProvider,
        ITabularDataPicker tabularDataPicker,
        IParameterPickerService parameterPickerService,
        IParameterFactory parameterFactory,
        bool byIndex) : base(databaseProvider, itemFromListProvider, tabularDataPicker, parameterPickerService, parameterFactory, byIndex)
    {
    }

    public async Task<string> ToStringAsync(long value, CancellationToken token, SmartBaseElement context) 
        => await ToStringAsync(value, token, (object?)context);

    public async Task<string> ToStringAsync(long value, CancellationToken token, IConditionViewModel context)
        => await ToStringAsync(value, token, (object?)context);

    public string ToString(long value, SmartBaseElement context) => ToString(value);
    public string ToString(long value, IConditionViewModel context) => ToString(value);
    
    public IEnumerable<int> AffectedByParameters()
    {
        yield return 0;
    }

    protected override long? GetQuestIdFromContext(object? context)
    {
        if (context is SmartBaseElement smartBaseElement)
            return smartBaseElement.GetParameter(0).Value;
        if (context is IConditionViewModel conditionViewModel)
            return conditionViewModel.GetParameter(0).Value;
        return null;
    }
}