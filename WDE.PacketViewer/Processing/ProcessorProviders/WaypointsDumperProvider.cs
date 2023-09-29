using System;
using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Processing.Processors.Utils;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [AutoRegister]
    public class WaypointsDumperProvider : IDocumentPacketDumperProvider
    {
        private readonly Func<WaypointsToDocumentProcessor> factory;

        public WaypointsDumperProvider(Func<WaypointsToDocumentProcessor> factory)
        {
            this.factory = factory;
        }
        public string Name => "Creature waypoints dump";
        public string Description => "Generate all waypoints per each unit in sniff [ALPHA]";
        public string Extension => "sql";// "waypoints";
        public ImageUri? Image { get; } = new ImageUri("Icons/document_waypoints_big.png");
        public Task<IPacketDocumentDumper> CreateDumper(IParsingSettings settings) =>
            Task.FromResult<IPacketDocumentDumper>(factory());
    }
}