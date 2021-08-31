using WowPacketParser.Proto;

namespace WDE.PacketViewer.Utils
{
    public static class GuidExtensions
    {
        public static string ToHexString(this UniversalGuid guid)
        {
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid64)
                return $"0x" + guid.Guid64.High.ToString("X8") + guid.Guid64.Low.ToString("X8");
            
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid128)
                return $"0x" + guid.Guid128.High.ToString("X16") + guid.Guid128.Low.ToString("X16");
            
            return "0x0000000000000000";
        }
    }
}