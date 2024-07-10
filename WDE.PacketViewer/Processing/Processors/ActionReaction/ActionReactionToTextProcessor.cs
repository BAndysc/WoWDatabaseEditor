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
            IChatEmoteSoundProcessor chatEmoteSoundProcessor, 
            IWaypointProcessor waypointsProcessor, 
            IUnitPositionFollower unitPositionFollower,
            IUpdateObjectFollower updateObjectFollower,
            IPlayerGuidFollower playerGuidFollower,
            IAuraSlotTracker auraSlotTracker) : base(actionGenerator, eventDetectorProcessor, chatEmoteSoundProcessor, waypointsProcessor, unitPositionFollower, updateObjectFollower, playerGuidFollower, auraSlotTracker)
        {
        }

        public override bool Process(ref readonly PacketHolder packet)
        {
            base.Process(in packet);
            var action = GetAction(packet.BaseData);
            if (!action.HasValue)
                return false;
            
            var eventAction = GetPossibleEventsForAction(packet.BaseData);
            
            //(act: {r.Item2*100:0}%, time: {r.Item3*100:0}%, dist: {r.Item4*100:0}%)
            var mainActor = action.Value.MainActor ?? default;
            var mainActorString = action.Value.MainActor != null ? mainActor.ToHexString() : "0x0";
            sb.AppendLine($"[{action.Value.PacketNumber}] {action.Value.Description} [{mainActorString}] because:");
            foreach (var e in eventAction)
            {
                var extraActor = e.Item2.MainActor ?? default;
                var extraActorString = e.Item2.MainActor != null ? extraActor.ToHexString() : "0x0";
                sb.AppendLine($"  - {e.Item1.Value * 100:0}% ({e.Item1.Explain}): [{e.Item2.PacketNumber}] {e.Item2.Description} [{extraActorString}]");
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