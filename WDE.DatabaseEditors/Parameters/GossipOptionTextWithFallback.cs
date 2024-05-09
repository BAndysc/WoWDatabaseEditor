using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

public class GossipOptionTextWithFallback : StringParameter, IAsyncContextualParameter<string, DatabaseEntity>,
    ICustomPickerContextualParameter<string>,
    ITableAffectedByParameter
{
    private readonly IDatabaseProvider databaseProvider;

    public GossipOptionTextWithFallback(IWindowManager windowManager, IDatabaseProvider databaseProvider) : base(windowManager)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public async Task<string> ToStringAsync(string value, CancellationToken token, DatabaseEntity context)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;
        
        var broadcastTextId = context.GetTypedValueOrThrow<long>(new ColumnFullName(null, "broadcasttext_option_text"));
        if (broadcastTextId == 0)
            return "";

        var text = await databaseProvider.GetBroadcastTextByIdAsync((uint)broadcastTextId);
        return text?.FirstText() ?? "";
    }

    public string ToString(string value, DatabaseEntity context)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        var broadcastTextId = context.GetTypedValueOrThrow<long>(new ColumnFullName(null, "broadcasttext_option_text"));
        if (broadcastTextId != 0)
            return $"BroadcastText {broadcastTextId} (fetching)";
        return "";
    }

    public async Task<(string, bool)> PickValue(string value, object context)
    {
        var vm = new StringPickerViewModel(context is DatabaseEntity entity ? await ToStringAsync(value, default, entity) : value,
            true, true);
        if (await windowManager.ShowDialog(vm))
        {
            return (vm.Content, true);
        }
        return ("", false);
    }

    public ColumnFullName AffectedByColumn => new ColumnFullName(null, "broadcasttext_option_text");

    public string ToString(string value, object context)
    {
        if (context is DatabaseEntity entity)
            return ToString(value, entity);
        return value;
    }

    public async Task<string> ToStringAsync(string value, CancellationToken token, object context)
    {
        if (context is DatabaseEntity entity)
            return await ToStringAsync(value, token, entity);
        return value;
    }
}