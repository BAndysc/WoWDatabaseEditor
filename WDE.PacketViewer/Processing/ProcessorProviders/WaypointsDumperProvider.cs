using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class WaypointsDumperProvider : IPacketDumperProvider
    {
        public WaypointsDumperProvider()
        {
        }
        public string Name => "Creature waypoints dump";
        public string Description => "Generate all waypoints per each unit in sniff [ALPHA]";
        public string Extension => "txt";
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(new WaypointsToTextProcessor(new WaypointsProcessor()));
    }
}