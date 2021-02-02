using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.AzerothCore
{
    [AutoRegister]
    [SingleInstance]
    public class AzerothCoreVersion : ICoreVersion
    {
        public string Tag => "Azeroth";
        public string FriendlyName => "AzerothCore Wrath of the Lich King";
    }
}