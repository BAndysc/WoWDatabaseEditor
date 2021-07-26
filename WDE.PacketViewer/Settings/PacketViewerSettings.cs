using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.PacketViewer.PacketParserIntegration;

namespace WDE.PacketViewer.Settings
{
    [AutoRegister]
    [SingleInstance]
    public class PacketViewerSettings : IPacketViewerSettings
    {
        private PacketViewerSettingsData data;
        private readonly IUserSettings userSettings;

        public PacketViewerSettings(IUserSettings userSettings)
        {
            this.userSettings = userSettings;
            data = userSettings.Get(new PacketViewerSettingsData(){Parser = ParserConfiguration.Defaults});
        }

        public PacketViewerSettingsData Settings
        {
            get => data;
            set
            {
                data = value;
                userSettings.Update(data);
            }
        }
    }
}