using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.Utils
{
    public class WaypointsToTextProcessor : IPacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IWaypointProcessor waypointProcessor;
        private readonly IParsingSettings parsingSettings;

        public WaypointsToTextProcessor(IWaypointProcessor waypointProcessor, IParsingSettings parsingSettings)
        {
            this.waypointProcessor = waypointProcessor;
            this.parsingSettings = parsingSettings;
        }

        public void Initialize(ulong gameBuild)
        {
            waypointProcessor.Initialize(gameBuild);
        }

        public bool Process(PacketHolder packet) => waypointProcessor.Process(packet);

        public async Task<string> Generate()
        {
            StringBuilder sb = new();
            Dictionary<UniversalGuid, float> randomnessMap = new();
            foreach (var unit in waypointProcessor.State)
                randomnessMap[unit.Key] = waypointProcessor.RandomMovementPacketRatio(unit.Key);

            Dictionary<uint, int> pathsPerEntry = new();
            
            foreach (var unit in waypointProcessor
                         .State
                         .OrderBy(pair => randomnessMap[pair.Key]))
            {
                if (unit.Key.Type == UniversalHighGuid.Player)
                    continue;
                
                if (unit.Value.Paths.Count == 0)
                    continue;

                var randomness = randomnessMap[unit.Key];

                var exporter = parsingSettings.WaypointsExporter;

                if (!pathsPerEntry.TryGetValue(unit.Key.Entry, out var basePathId))
                    basePathId = pathsPerEntry[unit.Key.Entry] = 0;
                
                if (exporter == null)
                    throw new Exception("Please select waypoint output type in parsing settings.");
                else
                    await exporter.Export(sb, basePathId, unit.Key, unit.Value, randomness);

                pathsPerEntry[unit.Key.Entry] += unit.Value.Paths.Count;
            }

            return sb.ToString();
        }
    }
}