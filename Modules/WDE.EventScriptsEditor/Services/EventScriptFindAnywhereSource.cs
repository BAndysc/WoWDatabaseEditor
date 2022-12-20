using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.EventScriptsEditor.EventScriptData;
using WDE.EventScriptsEditor.Solutions;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database.World;

namespace WDE.EventScriptsEditor.Services;

[AutoRegister]
[SingleInstance]
public class EventScriptFindAnywhereSource : IFindAnywhereSource
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IEventScriptDataProvider dataProvider;
    private readonly IEventScriptViewModelFactory viewModelFactory;

    public EventScriptFindAnywhereSource(IDatabaseProvider databaseProvider,
        IEventScriptDataProvider dataProvider,
        IEventScriptViewModelFactory viewModelFactory)
    {
        this.databaseProvider = databaseProvider;
        this.dataProvider = dataProvider;
        this.viewModelFactory = viewModelFactory;
    }

    public FindAnywhereSourceType SourceType => FindAnywhereSourceType.EventAi;

    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames, long parameterValue, CancellationToken cancellationToken)
    {
        EventScriptType? lookForType = null;
        if (parameterNames.IndexOf("EventScriptParameter") != -1)
            lookForType = EventScriptType.Event;
        else if (parameterNames.IndexOf("SpellParameter") != -1)
            lookForType = EventScriptType.Spell;

        if (lookForType.HasValue)
        {
            var result = await databaseProvider.GetEventScript(lookForType.Value, (uint)parameterValue);
            if (result.Count > 0)
            {
                var item = new EventScriptSolutionItem(lookForType.Value, (uint)parameterValue);
                var vm = viewModelFactory.Factory(result[0]);
                resultContext.AddResult(new FindAnywhereResult(
                    new ImageUri("Icons/document_event_script_big.png"),
                    null,
                    lookForType.Value + " Script",
                    vm.Text,
                    item
                ));
            }
        }
        
        var found = await databaseProvider.FindEventScriptLinesBy(GenerateConditions(parameterNames, parameterValue).ToList());
        foreach (var f in found)
        {
            var item = new EventScriptSolutionItem(f.Type, f.Id);
            var vm = viewModelFactory.Factory(f);
            resultContext.AddResult(new FindAnywhereResult(
                new ImageUri("Icons/document_event_script_big.png"),
                null,
                f.Type + " Script",
                vm.Text,
                item
                ));
        }
    }

    private IEnumerable<(uint command, int dataIndex, long valueToSearch)> GenerateConditions(IReadOnlyList<string> parameterName, long parameterValue)
    {
        foreach (var d in dataProvider.GetEventScriptData())
        {
            if (parameterName.IndexOf(d.DataLong) != -1)
                yield return (d.Id, 0, parameterValue);
                
            if (parameterName.IndexOf(d.DataLong2) != -1)
                yield return (d.Id, 1, parameterValue);
            
            if (parameterName.IndexOf(d.DataInt) != -1)
                yield return (d.Id, 2, parameterValue);
        }
    }
}