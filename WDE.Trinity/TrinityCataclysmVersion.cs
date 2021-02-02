using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityCataclysmVersion : ICoreVersion
    {
        public string Tag => "TrinityCata";
        public string FriendlyName => "The Cataclysm Preservation Project";
    }
}