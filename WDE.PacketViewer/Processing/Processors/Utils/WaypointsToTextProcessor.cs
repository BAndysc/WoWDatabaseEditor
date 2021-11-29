using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Utils;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.Utils
{
    public class WaypointsToTextProcessor : IPacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IWaypointProcessor waypointProcessor;

        public WaypointsToTextProcessor(IWaypointProcessor waypointProcessor)
        {
            this.waypointProcessor = waypointProcessor;
        }

        public bool Process(PacketHolder packet) => waypointProcessor.Process(packet);

        public async Task<string> Generate()
        {
            StringBuilder sb = new();
            Dictionary<UniversalGuid, float> randomnessMap = new();
            foreach (var unit in waypointProcessor.State)
                randomnessMap[unit.Key] = waypointProcessor.RandomMovementPacketRatio(unit.Key);

            foreach (var unit in waypointProcessor
                         .State
                         .OrderBy(pair => randomnessMap[pair.Key]))
            {
                if (unit.Value.Paths.Count == 0)
                    continue;

                var randomness = waypointProcessor.RandomMovementPacketRatio(unit.Key);
                sb.AppendLine("Creature " + unit.Key.ToWowParserString() + $" (entry: {unit.Key.Entry})  randomness: {(randomness) * 100:0.00}%");
                int i = 0;
                foreach (var path in unit.Value.Paths)
                {
                    if (!path.IsContinuationAfterPause)
                        sb.AppendLine(" (despawn) ");
                    sb.AppendLine("  Path " + i++);

                    int j = 0;
                    foreach (var segment in path.Segments)
                    {
                        sb.AppendLine("  Segment " + j++);
                        foreach (var p in segment.Waypoints)
                        {
                            sb.AppendLine($"    ({p.X}, {p.Y}, {p.Z})");
                        }
                    }

                    sb.AppendLine("");
                }
            }

            return sb.ToString();
        }
    }
}