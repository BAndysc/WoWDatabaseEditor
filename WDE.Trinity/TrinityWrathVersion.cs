using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityWrathVersion : ICoreVersion
    {
        public string Tag => "TrinityWrath";
        public string FriendlyName => "TrinityCore Wrath of the Lich King";
    }
}