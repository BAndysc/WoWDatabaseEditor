using System.Text;
using System.Threading.Tasks;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.ActionReaction
{
    public class ActionReactionToTextProcessor : ActionReactionProcessor, IPacketTextDumper
    {
        private StringBuilder sb = new();

        public bool RequiresSplitUpdateObject => true;

        public ActionReactionToTextProcessor(ActionGenerator actionGenerator,
            EventDetectorProcessor eventDetectorProcessor, 
            IRandomMovementDetector randomMovementDetector, 
            IChatEmoteSoundProcessor chatEmoteSoundProcessor, 
            IWaypointProcessor waypointsProcessor, 
            IUnitPositionFollower unitPositionFollower,
            IUpdateObjectFollower updateObjectFollower,
            IPlayerGuidFollower playerGuidFollower,
            IAuraSlotTracker auraSlotTracker) : base(actionGenerator, eventDetectorProcessor, randomMovementDetector, chatEmoteSoundProcessor, waypointsProcessor, unitPositionFollower, updateObjectFollower, playerGuidFollower, auraSlotTracker)
        {
        }

        public override bool Process(PacketHolder packet)
        {
            base.Process(packet);
            var action = GetAction(packet.BaseData);
            if (!action.HasValue)
                return false;
            
            var eventAction = GetPossibleEventsForAction(packet.BaseData);
            
            //(act: {r.Item2*100:0}%, time: {r.Item3*100:0}%, dist: {r.Item4*100:0}%)
            sb.AppendLine($"[{action.Value.PacketNumber}] {action.Value.Description} [{action.Value.MainActor?.ToHexString()}] because:");
            foreach (var e in eventAction)
            {
                sb.AppendLine($"  - {e.Item1.Value * 100:0}% ({e.Item1.Explain}): [{e.Item2.PacketNumber}] {e.Item2.Description} [{e.Item2.MainActor?.ToHexString()}]");
            }
            sb.AppendLine();
            
            return true;
        }

        public async Task<string> Generate()
        {
            return sb.ToString();
        }
    }
}