using WDE.Common.Services;
using WDE.PacketViewer.PacketParserIntegration;

namespace WDE.PacketViewer.Settings
{
    public readonly struct PacketViewerSettingsData : ISettings
    {
        public bool AlwaysSplitUpdates { get; init; }
        public bool WrapLines { get; init; }
        public string? DefaultFilter { get; init; }
        public ParserConfiguration Parser { get; init; }
    }
}