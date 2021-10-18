using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.CustomCommands
{
    [UniqueProvider]
    public interface IDatabaseTableCommandService
    {
        IDatabaseTableCommand? FindCommand(string id);
        IDatabaseTablePerKeyCommand? FindPerKeyCommand(string id);
    }
}