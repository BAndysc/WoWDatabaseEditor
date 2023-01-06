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
    private readonly string tableName;
    private readonly string columnName;

    public CreatureTextParameter(IDatabaseProvider databaseProvider,
        ITableEditorPickerService tableEditorPickerService,
        IItemFromListProvider itemFromListProvider,
        string tableName, string columnName)
    {
        this.databaseProvider = databaseProvider;
        this.tableEditorPickerService = tableEditorPickerService;
        this.itemFromListProvider = itemFromListProvider;
        this.tableName = tableName;
        this.columnName = columnName;
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

    private uint? GetEntry(SmartBaseElement? element)
    {
        var script = GetScript(element);
        if (script == null)
            return null;
        if (element is SmartAction action)
        {
            if ((action.GetParameter(2).Value != 0 ||
                action.Source.Id == SmartConstants.SourceNone ||
                action.Source.Id == SmartConstants.SourceSelf)
                && script is SmartScript smartScript)
            {
                if (smartScript.EntryOrGuid >= 0)
                {
                    if (smartScript.SourceType is SmartScriptType.Template or SmartScriptType.TimedActionList)
                        return (uint)smartScript.EntryOrGuid / 100;
                    
                    return (uint)smartScript.EntryOrGuid;
                }

                return databaseProvider.GetCreatureByGuid((uint)(-smartScript.EntryOrGuid))?.Entry;
            }
            else if (action.Source.Id == 9 || action.Source.Id == 59) // creature range or creature by spawn key
                return (uint)action.Source.GetParameter(0).Value;
            else if (action.Source.Id == 10) // creature guid
                return databaseProvider.GetCreatureByGuid((uint)action.Source.GetParameter(0).Value)?.Entry;
            else if (action.Source.Id == 11) // creature distance
                return (uint)action.Source.GetParameter(0).Value;
            else if (action.Source.Id == 19) // closest creature
                return (uint)action.Source.GetParameter(0).Value;
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
        var entry = GetEntry(context as SmartBaseElement);
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