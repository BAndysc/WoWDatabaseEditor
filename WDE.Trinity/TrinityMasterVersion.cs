using WDE.Common.CoreVersion;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityMasterVersion : ICoreVersion
    {
        public string Tag => "TrinityMaster";
        public string FriendlyName => "TrinityCore Shadowlands";
    }
}