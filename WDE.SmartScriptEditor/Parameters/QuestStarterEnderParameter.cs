using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class QuestStarterEnderParameter : IParameter<long>, ICustomPickerContextualParameter<long>
{
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IQuestEntryProviderService questEntryProviderService;
    private readonly string tableSuffix;

    public QuestStarterEnderParameter(ICachedDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService,
        IQuestEntryProviderService questEntryProviderService,
        string tableSuffix)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
        this.questEntryProviderService = questEntryProviderService;
        this.tableSuffix = tableSuffix;
    }

    public bool AllowUnknownItems => true;
    
    private static SmartScript? GetScript(SmartBaseElement? element)
    {
        if (element is SmartSource source)
            return source.Parent?.Parent?.Parent as SmartScript;
        if (element is SmartAction action)
            return action.Parent?.Parent as SmartScript;
        if (element is SmartEvent @event)
            return @event.Parent as SmartScript;
        if (element is SmartCondition @condition)
            return @condition.Parent?.Parent as SmartScript;
        return null;
    }

    private async ValueTask<uint?> GetEntry(SmartBaseElement? element)
    {
        var script = GetScript(element);
        if (script == null)
            return null;
        if (script.EntryOrGuid < 0)
        {
            if (script.SourceType == SmartScriptType.GameObject)
                return (await databaseProvider.GetGameObjectByGuidAsync(0, (uint)(-script.EntryOrGuid)))?.Entry;
            else
                return (await databaseProvider.GetCreatureByGuidAsync(0, (uint)(-script.EntryOrGuid)))?.Entry;
        }

        uint value = 0;
        if (script.Entry.HasValue)
            value = script.Entry.Value;
        else
            value = (uint)script.EntryOrGuid;

        if (script.SourceType is SmartScriptType.Template or SmartScriptType.TimedActionList)
            value /= 100;

        return value;
    }

    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var entry = await GetEntry(context as SmartBaseElement);
        if (!entry.HasValue)
        {
            return await FallbackPicker();
        }
        else
        {
            var script = GetScript(context as SmartBaseElement);
            bool isGameObject = script!.SourceType == SmartScriptType.GameObject;
            var table = (isGameObject ? "gameobject" : "creature") + "_" + (tableSuffix);
            try
            {
                var id = await tableEditorPickerService.PickByColumn(DatabaseTable.WorldTable(table), new DatabaseKey(entry ?? 0), "quest", (uint)value);
                if (id.HasValue)
                    return (id.Value, true);
                return (0, false);
            }
            catch (UnsupportedTableException)
            {
                return await FallbackPicker();
            }
        }
    }

    private async Task<(long, bool)> FallbackPicker()
    {
        var item = await questEntryProviderService.GetEntryFromService();
        if (item.HasValue)
            return (item.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;

    public Dictionary<long, SelectOption>? Items { get; set; }
    
    public string ToString(long value)
    {
        var text = databaseProvider.GetCachedQuestTemplate((uint)value);
        if (text == null)
            return value.ToString();
        return $"{text.Name} ({value})";
    }
}