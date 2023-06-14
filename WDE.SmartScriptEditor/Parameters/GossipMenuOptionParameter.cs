using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Conditions.Shared;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class GossipMenuOptionParameter : IContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>,
    IAffectedByOtherParametersParameter
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IItemFromListProvider itemFromListProvider;

    public GossipMenuOptionParameter(IDatabaseProvider databaseProvider,
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

    private uint? GetMenuEntry(SmartBaseElement? element)
    {
        if (element is SmartEvent ev)
        {
            if (ev.GetParameter(0).Value > 0)
                return (uint)ev.GetParameter(0).Value;
        }
        
        var script = GetScript(element);
        if (script == null)
            return null;
        
        uint? entry = 0;
        if (script.EntryOrGuid < 0)
            entry = databaseProvider.GetCreatureByGuid(0, (uint)(-script.EntryOrGuid))?.Entry;
        else if (script.Entry.HasValue)
            entry = script.Entry.Value;
        else
            entry = (uint)script.EntryOrGuid;
        
        if (!entry.HasValue)
            return null;

        return databaseProvider.GetCreatureTemplate(entry.Value)?.GossipMenuId;
    }

    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var entry = GetMenuEntry(context as SmartBaseElement);
        if (!entry.HasValue)
        {
            return await FallbackPicker(value);
        }
        else
        {
            try
            {
                var id = await tableEditorPickerService.PickByColumn("gossip_menu_option", new DatabaseKey(entry.Value), "OptionID", (uint)value, "id");
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
        var item = await itemFromListProvider.GetItemFromList(null, false, value, "Pick gossip menu option");
        if (item.HasValue)
            return (item.Value, true);
        return (0, false);
    }

    public string? Prefix => null;
    public bool HasItems => true;

    public Dictionary<long, SelectOption>? Items { get; set; }
    
    public string ToString(long value, SmartBaseElement context)
    {
        var entry = GetMenuEntry(context);
        if (entry == null)
            return value.ToString();
        var options = databaseProvider.GetGossipMenuOptions(entry.Value);
        if (options == null || options.Count == 0)
            return value.ToString();
        var firstOrDefault = options.FirstOrDefault(x => x.OptionIndex == value);
        return firstOrDefault == null ? value.ToString() : $"{firstOrDefault.Text?.TrimToLength(25)} ({value})";
    }
    
    public string ToString(long value)
    {
        return value.ToString();
    }

    private static int[] affectedBy => new[] { 0 }; // gossip menu id
    public IEnumerable<int> AffectedByParameters() => affectedBy;
}