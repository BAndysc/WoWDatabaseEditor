using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class CreatureTextParameter : IContextualParameter<long, SmartBaseElement>, IAsyncContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly DatabaseTable tableName;
    private readonly string columnName;

    private Dictionary<int, int> targetIdToCreatureEntryParameter = new();
    private Dictionary<int, int> actionIdToTalkTarget = new();

    public CreatureTextParameter(ISmartDataManager smartDataManager,
        IDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService,
        IItemFromListProvider itemFromListProvider,
        DatabaseTable tableName, string columnName)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
        this.itemFromListProvider = itemFromListProvider;
        this.tableName = tableName;
        this.columnName = columnName;

        smartDataManager.GetAllData(SmartType.SmartAction).SubscribeAction(LoadActions);
        smartDataManager.GetAllData(SmartType.SmartTarget).SubscribeAction(Load);
    }

    private void LoadActions(IReadOnlyList<SmartGenericJsonData> actions)
    {
        actionIdToTalkTarget = actions
            .Where(x => x.Parameters != null && x.Parameters.Any(p => p.Name == "UseTalkTarget"))
            .Select(x => (x.Id, x.Parameters!.IndexIf(p => p.Name == "UseTalkTarget")))
            .ToDictionary(x => x.Id, x => x.Item2);
    }

    private void Load(IReadOnlyList<SmartGenericJsonData> targets)
    {
        foreach (var data in targets)
        {
            if (data.Parameters == null)
                continue;

            for (int i = 0; i < data.Parameters.Count; ++i)
            {
                if (data.Parameters[i].Type == "CreatureParameter")
                    targetIdToCreatureEntryParameter[data.Id] = i;
            }
        }
    }

    public bool AllowUnknownItems => true;
    
    private static SmartScriptBase? GetScript(SmartBaseElement? element)
    {
        if (element is SmartSource source)
            return source.Parent?.Parent?.Parent;
        if (element is SmartAction action)
            return action.Parent?.Parent;
        if (element is SmartEvent @event)
            return @event.Parent;
        if (element is SmartCondition @condition)
            return @condition.Parent?.Parent;
        return null;
    }

    private async ValueTask<uint?> GetEntry(SmartBaseElement? element)
    {
        async ValueTask<uint?> GetCreatureEntryFromScript(SmartScript smartScript)
        {
            if (smartScript.Entry.HasValue)
            {
                if (smartScript.SourceType is SmartScriptType.Template or SmartScriptType.TimedActionList)
                    return smartScript.Entry.Value / 100;

                return smartScript.Entry.Value;
            }
            
            if (smartScript.EntryOrGuid >= 0)
            {
                if (smartScript.SourceType is SmartScriptType.Template or SmartScriptType.TimedActionList)
                    return (uint)smartScript.EntryOrGuid / 100;

                return (uint)smartScript.EntryOrGuid;
            }

            return (await databaseProvider.GetCreatureByGuidAsync(0, (uint)(-smartScript.EntryOrGuid)))?.Entry;
        }

        var script = GetScript(element);
        if (script == null || script is not SmartScript smartScript)
            return null;
        if (element is SmartEvent)
        {
            return await GetCreatureEntryFromScript(smartScript);
        }
        if (element is SmartAction action)
        {
            if (((actionIdToTalkTarget.TryGetValue(action.Id, out var talkTargetIndex)
                  && action.GetParameter(talkTargetIndex).Value != 0) ||
                action.Source.Id == SmartConstants.SourceNone ||
                action.Source.Id == SmartConstants.SourceSelf))
            {
                return await GetCreatureEntryFromScript(smartScript);
            }
            else if (targetIdToCreatureEntryParameter.TryGetValue(action.Source.Id, out var creatureEntryParameterIndex))
                return (uint)action.Source.GetParameter(creatureEntryParameterIndex).Value;
            else if (action.Source.Id == 10) // creature guid
                return (await databaseProvider.GetCreatureByGuidAsync(0, (uint)action.Source.GetParameter(0).Value))?.Entry;
            else if (action.Source.Id == 12 || action.Source.Id == 58) // stored target / actor
            {
                var val = action.Source.GetParameter(0).Value;
                var type = action.Source.Id == 12 ? GlobalVariableType.StoredTarget : GlobalVariableType.Actor;
                var found = script.GlobalVariables.FirstOrDefault(x => x.Key == val && x.VariableType == type);
                return found == null || found.Entry == 0 ? null : found.Entry;
            }
        }
        return null;
    }

    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var entry = await GetEntry(context as SmartBaseElement);
        if (!entry.HasValue)
        {
            return await FallbackPicker(value);
        }
        else
        {
            try
            {
                var id = await tableEditorPickerService.PickByColumn(tableName, new DatabaseKey(entry ?? 0), columnName,
                    (uint)value);
                if (id.HasValue)
                    return (id.Value, true);
                return (0, false);
            }
            catch (UnsupportedTableException)
            {
                return await FallbackPicker(value);
            }
        }
    }

    private async Task<(long, bool)> FallbackPicker(long value)
    {
        var item = await itemFromListProvider.GetItemFromList(null, false, value, "Pick creature text group id");
        if (item.HasValue)
            return (item.Value, true);
        return (0, false);
    }

    public async Task<string> ToStringAsync(long value, CancellationToken token, object context)
    {
        if (context is SmartBaseElement smartBaseElement)
            return await ToStringAsync(value, token, smartBaseElement);
        return value.ToString();
    }

    public string? Prefix => null;
    public bool HasItems => true;

    public Dictionary<long, SelectOption>? Items { get; set; }
    
    public async Task<string> ToStringAsync(long value, CancellationToken token, SmartBaseElement context)
    {
        var entry = await GetEntry(context);
        if (entry == null)
            return value.ToString();
        
        var texts = await databaseProvider.GetCreatureTextsByEntryAsync(entry.Value);
        if (texts.Count == 0)
            return value.ToString();
        
        var firstOrDefault = texts.FirstOrDefault(x => x.GroupId == value);

        if (firstOrDefault == null)
            return value.ToString();

        var text = firstOrDefault.Text;

        if (string.IsNullOrWhiteSpace(text) && firstOrDefault.BroadcastTextId > 0)
        {
            var broadcastText = await databaseProvider.GetBroadcastTextByIdAsync(firstOrDefault.BroadcastTextId);
            text = broadcastText?.FirstText() ?? "";
        }
    
        if (text != null)
           return text.TrimToLength(60) + $" ({value})";

        return value.ToString();
    }

    public string ToString(long value, SmartBaseElement context) 
        => value.ToString();

    public string ToString(long value) 
        => value.ToString();

    public string ToString(long value, object context)
    {
        if (context is SmartBaseElement smartBaseElement)
            return ToString(value, smartBaseElement);
        return ToString(value);
    }
}
