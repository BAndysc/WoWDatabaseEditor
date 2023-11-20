using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;

namespace WDE.DatabaseEditors.Parameters;

public class GameObjectGUIDPickerOnlyParameter : IParameter<long>, ICustomPickerParameter<long>
{
    protected readonly IDatabaseProvider databaseProvider;
    protected readonly ITableEditorPickerService editorPickerService;
    public string? Prefix => null;
    public bool HasItems => true;
    public bool AllowUnknownItems => true;
    public Dictionary<long, SelectOption>? Items => null;

    public GameObjectGUIDPickerOnlyParameter(IDatabaseProvider databaseProvider,
        ITableEditorPickerService editorPickerService)
    {
        this.databaseProvider = databaseProvider;
        this.editorPickerService = editorPickerService;
    }

    public virtual string ToString(long value)
    {
        return value.ToString();
    }

    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await editorPickerService.PickByColumn(DatabaseTable.WorldTable("gameobject"), new DatabaseKey(value), "guid", null);
        return (result ?? 0, result.HasValue);
    }
}

public class GameObjectGUIDParameter : GameObjectGUIDPickerOnlyParameter, IAsyncParameter<long>
{
    public GameObjectGUIDParameter(IDatabaseProvider databaseProvider,
        ITableEditorPickerService editorPickerService) : base(databaseProvider, editorPickerService)
    {
    }

    public async Task<string> ToStringAsync(long val, CancellationToken token)
    {
        var creature = await databaseProvider.GetGameObjectByGuidAsync(0, (uint)val);
        if (creature == null)
            return val + " (not found)";

        var template = await databaseProvider.GetGameObjectTemplate(creature.Entry);

        if (template == null)
            return val.ToString();
        
        return $"{val} ({template.Name} - {template.Entry})";
    }

    public override string ToString(long value)
    {
        return value + " (fetching)";
    }
}