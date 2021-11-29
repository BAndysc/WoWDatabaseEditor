using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Processing.Processors.Utils;

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
        public ImageUri? Image { get; } = new ImageUri("Icons/document_waypoints_big.png");
        public Task<IPacketTextDumper> CreateDumper() =>
            Task.FromResult<IPacketTextDumper>(new WaypointsToTextProcessor(new WaypointsProcessor()));
    }
}