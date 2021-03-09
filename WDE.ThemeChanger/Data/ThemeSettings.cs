using WDE.Common.Services;

namespace WDE.ThemeChanger.Data
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