using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services
{
    [UniqueProvider]
    public interface IProgramNameService
    {
        string Title { get; }
    }
}