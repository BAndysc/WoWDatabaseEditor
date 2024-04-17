using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IStandaloneTableEditService
{
    void OpenEditor(DatabaseTable tableId, DatabaseKey? key = null, string? customWhere = null);
    void OpenTemplatesEditor(IReadOnlyList<DatabaseKey> keys, DatabaseTable tableId);
    void OpenMultiRecordEditor(IReadOnlyList<DatabaseKey> partialKeys, DatabaseTable table, params DatabaseTable[] fallbackTables);
}