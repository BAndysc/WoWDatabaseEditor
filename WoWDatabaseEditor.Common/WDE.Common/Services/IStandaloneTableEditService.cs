using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IStandaloneTableEditService
{
    void OpenEditor(DatabaseTable tableId, DatabaseKey? key = null, string? customWhere = null);
}