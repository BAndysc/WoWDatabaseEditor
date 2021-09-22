using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Avalonia.Services.AppearanceService.Data
{
    public struct ThemeSettings : ISettings
    {
        public string Name { get; }
        
        public bool UseCustomScaling { get; }

        public double CustomScaling { get; }

        public ThemeSettings(string name, bool useCustomScaling, double customScaling)
        {
            Name = name;
            UseCustomScaling = useCustomScaling;
            CustomScaling = customScaling;
        }
    }
}