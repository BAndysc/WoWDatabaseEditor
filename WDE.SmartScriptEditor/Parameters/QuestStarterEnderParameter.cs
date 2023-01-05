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
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IQuestEntryProviderService questEntryProviderService;
    private readonly string tableSuffix;

    public QuestStarterEnderParameter(IDatabaseProvider databaseProvider,
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

    private uint? GetEntry(SmartBaseElement? element)
    {
        var script = GetScript(element);
        if (script == null)
            return null;
        if (script.EntryOrGuid < 0)
        {
            if (script.SourceType == SmartScriptType.GameObject)
                return databaseProvider.GetGameObjectByGuid((uint)(-script.EntryOrGuid))?.Entry;
            else
                return databaseProvider.GetCreatureByGuid((uint)(-script.EntryOrGuid))?.Entry;
        }
        return (uint)script.EntryOrGuid;
    }

    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var entry = GetEntry(context as SmartBaseElement);
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
                var id = await tableEditorPickerService.PickByColumn(table, new DatabaseKey(entry ?? 0), "quest", (uint)value);
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
        var text = databaseProvider.GetQuestTemplate((uint)value);
        if (text == null)
            return value.ToString();
        return $"{text.Name} ({value})";
    }
}