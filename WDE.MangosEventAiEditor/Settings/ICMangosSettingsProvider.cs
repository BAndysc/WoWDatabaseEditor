using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Settings
{
    [UniqueProvider]
    public interface ICMangosSettingsProvider
    {
        string? DbRepositoryPath { get; set; }
        bool UpdateAcidFileOnSave { get; set; }
        void Apply();
    }
}
