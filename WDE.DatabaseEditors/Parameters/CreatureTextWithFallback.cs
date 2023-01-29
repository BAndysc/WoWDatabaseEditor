using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

public class CreatureTextWithFallback : StringParameter, IAsyncContextualParameter<string, DatabaseEntity>, ITableAffectedByParameter
{
    private readonly IDatabaseProvider databaseProvider;

    public CreatureTextWithFallback(IWindowManager windowManager, IDatabaseProvider databaseProvider) : base(windowManager)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public async Task<string> ToStringAsync(string value, CancellationToken token, DatabaseEntity context)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;
        
        var broadcastTextId = context.GetTypedValueOrThrow<long>("broadcasttextid");
        if (broadcastTextId == 0)
            return "";

        var text = await databaseProvider.GetBroadcastTextByIdAsync((uint)broadcastTextId);
        return text?.Text ?? text?.Text1 ?? "";
    }

    public string ToString(string value, DatabaseEntity context)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        var broadcastTextId = context.GetTypedValueOrThrow<long>("broadcasttextid");
        if (broadcastTextId != 0)
            return $"BroadcastText {broadcastTextId} (fetching)";
        return "";
    }

    public string AffectedByColumn => "broadcasttextid";
}