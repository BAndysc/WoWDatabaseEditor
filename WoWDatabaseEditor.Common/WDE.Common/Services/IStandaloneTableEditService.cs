using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IStandaloneTableEditService
{
    void OpenEditor(string tableId, DatabaseKey? key = null, string? customWhere = null);
}