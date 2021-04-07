using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Data
{
    public struct ThemeSettings : ISettings
    {
        public string Name { get; }

        public ThemeSettings(string name)
        {
            Name = name;
        }
    }
}