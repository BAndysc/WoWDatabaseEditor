using WDE.Common.CoreVersion;

namespace WoWDatabaseEditor.CoreVersion
{
    public interface ICurrentCoreSettings
    {
        string? CurrentCore { get; }
        void UpdateCore(ICoreVersion core);
    }
}