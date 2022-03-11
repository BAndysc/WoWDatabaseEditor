using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class CreatureTextParameter : IContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IItemFromListProvider itemFromListProvider;

    public CreatureTextParameter(IDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService,
        IItemFromListProvider itemFromListProvider)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
        this.itemFromListProvider = itemFromListProvider;
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
        if (element is SmartAction action)
        {
            if (action.GetParameter(2).Value != 0 ||
                action.Source.Id == SmartConstants.SourceNone ||
                action.Source.Id == SmartConstants.SourceSelf)
            {
                if (script.EntryOrGuid >= 0)
                    return (uint)script.EntryOrGuid;
                return databaseProvider.GetCreatureByGuid((uint)(-script.EntryOrGuid))?.Entry;
            }
            else if (action.Source.Id == 9 || action.Source.Id == 59) // creature range or creature by spawn key
                return (uint)action.Source.GetParameter(0).Value;
            else if (action.Source.Id == 10) // creature guid
                return databaseProvider.GetCreatureByGuid((uint)action.Source.GetParameter(0).Value)?.Entry;
            else if (action.Source.Id == 11) // creature distance
                return (uint)action.Source.GetParameter(0).Value;
            else if (action.Source.Id == 19) // closest creature
                return (uint)action.Source.GetParameter(0).Value;
        }
        return null;
    }

    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var entry = GetEntry(context as SmartBaseElement);
        if (!entry.HasValue)
        {
            return await FallbackPicker(value);
        }
        else
        {
            try
            {
                var id = await tableEditorPickerService.PickByColumn("creature_text", new DatabaseKey(entry ?? 0), "GroupId",
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

    public string? Prefix => null;
    public bool HasItems => true;

    public Dictionary<long, SelectOption>? Items { get; set; }
    
    public string ToString(long value, SmartBaseElement context)
    {
        var entry = GetEntry(context);
        if (entry == null)
            return value.ToString();
        var text = databaseProvider.GetCreatureTextsByEntry(entry.Value);
        if (text == null || text.Count == 0)
            return value.ToString();
        var firstOrDefault = text.FirstOrDefault(x => x.GroupId == value);
        return firstOrDefault == null ? value.ToString() : $"{firstOrDefault.Text?.TrimToLength(60)} ({value})";
    }
    
    public string ToString(long value)
    {
        return value.ToString();
    }
}