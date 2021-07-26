using WDE.Module.Attributes;

namespace WDE.PacketViewer.Settings
{
    [UniqueProvider]
    public interface IPacketViewerSettings
    {
        PacketViewerSettingsData Settings { get; set; }
    }
}