using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [UniqueProvider]
    public interface IPlayerGuidFollower : IPacketProcessor<bool>
    {
        UniversalGuid? PlayerGuid { get; }
    }
    
    [AutoRegister]
    public class PlayerGuidFollower : IPlayerGuidFollower
    {
        public bool Process(ref readonly PacketHolder packet)
        {
            if (PlayerGuid != null)
                return false;

            if (packet.KindCase == PacketHolder.KindOneofCase.ClientMove)
            {
                PlayerGuid = packet.ClientMove.Mover;
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.PlayerLogin)
            {
                PlayerGuid = packet.PlayerLogin.PlayerGuid;
            }
            return true;
        }

        public UniversalGuid? PlayerGuid { get; private set; }
    }
}