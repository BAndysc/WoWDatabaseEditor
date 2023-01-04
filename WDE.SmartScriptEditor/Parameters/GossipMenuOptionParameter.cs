using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Conditions.Shared;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Parameters;

public class GossipMenuOptionParameter : BaseAsyncContextualParameter<long, SmartBaseElement>, ICustomPickerContextualParameter<long>,
    IAffectedByOtherParametersParameter
{
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IItemFromListProvider itemFromListProvider;

    public GossipMenuOptionParameter(ICachedDatabaseProvider databaseProvider,
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

    private async ValueTask<uint?> GetMenuEntry(SmartBaseElement? element)
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
            entry = (await databaseProvider.GetCreatureByGuidAsync(0, (uint)(-script.EntryOrGuid)))?.Entry;
        else if (script.Entry.HasValue)
            entry = script.Entry.Value;
        else
            entry = (uint)script.EntryOrGuid;

        if (!entry.HasValue)
            return null;

        return databaseProvider.GetCachedCreatureTemplate(entry.Value)?.GossipMenuId;
    }

    public async Task<(long, bool)> PickValue(long value, object context)
    {
        var entry = await GetMenuEntry(context as SmartBaseElement);
        if (!entry.HasValue)
        {
            return await FallbackPicker(value);
        }
        else
        {
            try
            {
                var id = await tableEditorPickerService.PickByColumn(DatabaseTable.WorldTable("gossip_menu_option"), new DatabaseKey(entry.Value), "OptionID", (uint)value, "id");
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

    public override string? Prefix => null;
    public override bool HasItems => true;

    public override Dictionary<long, SelectOption>? Items => null;

    public override async Task<string> ToStringAsync(long value, CancellationToken token, SmartBaseElement context)
    {
        var entry = await GetMenuEntry(context);
        if (entry == null)
            return value.ToString();
        var options = await databaseProvider.GetGossipMenuOptionsAsync(entry.Value);
        if (options == null || options.Count == 0)
            return value.ToString();
        var firstOrDefault = options.FirstOrDefault(x => x.OptionIndex == value);
        if (firstOrDefault == null)
            return value.ToString();
        var text = firstOrDefault.Text;
        if (text == null && firstOrDefault.BroadcastTextId > 0)
            text = (await databaseProvider.GetBroadcastTextByIdAsync((uint)firstOrDefault.BroadcastTextId))?.FirstText();
        return text == null ? value.ToString() : $"{text.TrimToLength(25)} ({value})";
    }

    public override string ToString(long value, SmartBaseElement context)
    {
        return "...loading (" + (value) + ")";
    }

    public override string ToString(long value)
    {
        return value.ToString();
    }

    private static int[] affectedBy => new[] { 0 }; // gossip menu id
    public IEnumerable<int> AffectedByParameters() => affectedBy;
}
