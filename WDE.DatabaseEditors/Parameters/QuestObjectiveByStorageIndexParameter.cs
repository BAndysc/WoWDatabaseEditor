using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.TableData;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.Parameters.Parameters;

namespace WDE.DatabaseEditors.Parameters;

public class QuestObjectiveByStorageIndexParameter : BaseQuestObjectiveParameter,
    IAsyncContextualParameter<long, DatabaseEntity>
{
    private readonly ColumnFullName questIdColumnName;

    public QuestObjectiveByStorageIndexParameter(ICachedDatabaseProvider databaseProvider, 
        IItemFromListProvider itemFromListProvider,
        ITabularDataPicker tabularDataPicker,
        IParameterPickerService parameterPickerService,
        IParameterFactory parameterFactory,
        ColumnFullName questIdColumnName) : base (databaseProvider, itemFromListProvider, tabularDataPicker, parameterPickerService, parameterFactory, true)
    {
        this.questIdColumnName = questIdColumnName;
    }
    
    protected override long? GetQuestIdFromContext(object? context)
    {
        if (context is not DatabaseEntity entity)
            return null;

        return entity.GetTypedValueOrThrow<long>(questIdColumnName);
    }

    public string ToString(long value, DatabaseEntity context) => ToString(value, (object)context);

    public async Task<string> ToStringAsync(long value, CancellationToken token, DatabaseEntity context) => await ToStringAsync(value, token, (object?)context);
}