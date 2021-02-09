using WDE.Common.CoreVersion;

namespace WoWDatabaseEditorCore.CoreVersion
{
    public interface ICurrentCoreSettings
    {
        string? CurrentCore { get; }
        void UpdateCore(ICoreVersion core);
    }
}