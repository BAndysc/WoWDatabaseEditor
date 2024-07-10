using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.PacketViewer.Processing.Processors.Paths.ViewModels;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.Utils
{
    public class WaypointsToDocumentProcessor : IPacketProcessor<bool>, IPacketDocumentDumper
    {
        private readonly IWaypointProcessor waypointProcessor;
        private readonly IParsingSettings parsingSettings;
        private readonly Func<SniffWaypointsDocumentViewModel> factory;

        public WaypointsToDocumentProcessor(IWaypointProcessor waypointProcessor, 
            IParsingSettings parsingSettings,
            Func<SniffWaypointsDocumentViewModel> factory)
        {
            this.waypointProcessor = waypointProcessor;
            this.parsingSettings = parsingSettings;
            this.factory = factory;
        }

        public void Initialize(ulong gameBuild)
        {
            waypointProcessor.Initialize(gameBuild);
        }

        public bool Process(ref readonly PacketHolder packet) => waypointProcessor.Process(in packet);

        public async Task<IDocument> Generate(PacketDocumentViewModel? packetDocumentViewModel)
        {
            Dictionary<UniversalGuid, float> randomnessMap = new();
            foreach (var unit in waypointProcessor.State)
                randomnessMap[unit.Key] = waypointProcessor.RandomMovementPacketRatio(unit.Key);

            var vm = factory();
            vm.Title = $"Sniff paths ({packetDocumentViewModel?.Title})";

            foreach (var (guid, movement) in waypointProcessor
                         .State
                         .OrderBy(pair => randomnessMap[pair.Key]))
            {
                if (guid.Type is not UniversalHighGuid.Creature and not UniversalHighGuid.Vehicle)
                    continue;
                
                if (movement.Paths.Count == 0)
                    continue;

                var randomness = randomnessMap[guid];

                await vm.AddCreaturePaths(guid, randomness, movement);
            }
            
            return vm;
        }
    }
}