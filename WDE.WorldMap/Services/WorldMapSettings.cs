using WDE.Common.Services;

namespace WDE.WorldMap.Services
{
    public readonly struct WorldMapSettings : ISettings
    {
        public string? Path { get; init; }
    }
}