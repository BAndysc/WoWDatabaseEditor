using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.PacketViewer.Services;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Processing.Processors.ActionReaction
{
    public interface IRelatedPacketsFinder
    {
        IFilterData Find(ulong gameBuild, IList<PacketViewModel> packets, IList<PacketViewModel> unfilteredPackets, int start, CancellationToken token);
    }
    
    [AutoRegister]
    [SingleInstance]
    public class RelatedPacketsFinder : IRelatedPacketsFinder
    {
        private readonly IActionReactionProcessorCreator actionReactionProcessorCreator;

        public RelatedPacketsFinder(IActionReactionProcessorCreator actionReactionProcessorCreator)
        {
            this.actionReactionProcessorCreator = actionReactionProcessorCreator;
        }

        public class NoRelatedActionsException : Exception
        {
            
        }
        
        public IFilterData Find(ulong gameBuild, IList<PacketViewModel> packets, IList<PacketViewModel> unfilteredPackets, int start, CancellationToken token)
        {
            var processor = actionReactionProcessorCreator.Create();
            EventHappened? happenReason = null;

            processor.Initialize(gameBuild);
            
            foreach (var f in packets)
            {
                processor.PreProcess(ref f.Packet);
                if (token.IsCancellationRequested)
                    throw new TaskCanceledException();
            }

            int i = 0;
            int j = 0;
            for (var index = 0; index < packets.Count; index++)
            {
                var packet = packets[index];

                while (j < unfilteredPackets.Count && unfilteredPackets[j].Id < packet.Id)
                    processor.ProcessUnfiltered(ref unfilteredPackets[j++].Packet);

                i++;
                j++;
                processor.Process(ref packet.Packet);
                if (token.IsCancellationRequested)
                    throw new TaskCanceledException();
            }

            // Firstly we go backwards to find first ever possible event
            // This will be treated as a general reason of packet
            int pid = start;
            happenReason = processor.GetPossibleActionsForEvent(start).Select(s => s.happened).FirstOrDefault();
            HashSet<int> visited = new();
            while (true)
            {
                if (!visited.Add(pid))
                    throw new NoRelatedActionsException();
                
                var action = processor.GetAction(pid);
                if (action == null)
                    break;

                var any = false;
                foreach (var reason in processor.GetPossibleEventsForAction(pid))
                {
                    var probability = reason.rate.Value;
                    
                    if (reason.@event.Kind == EventType.Movement && probability > 0.75 ||
                        reason.@event.Kind != EventType.Movement && probability > 0.55)
                    {
                        any = true;
                        happenReason = reason.@event;
                        pid = happenReason.Value.PacketNumber;
                        break;
                    }
                    else if (probability <= 0.55)
                        break; // reasons are sorted by value, so we can discard rest
                }

                if (!any)
                    break;
            }

            if (!happenReason.HasValue)
                throw new NoRelatedActionsException();
            
            Dictionary<int, PacketViewModel> idToPacket = new();
            foreach (var f in packets)
                idToPacket[f.Id] = f;

            LOG.LogInformation("First event in chain: {@description} (@number)",  happenReason!.Value.Description, happenReason.Value.PacketNumber);
            Queue<int> reasons = new Queue<int>();
            HashSet<int> usedReasons = new();
            List<int> relatedPacketsWithoutActors = new();
            HashSet<UniversalGuid> relatedActors = new HashSet<UniversalGuid>();
            if (happenReason.Value.MainActor != null)
                relatedActors.Add(happenReason.Value.MainActor.Value);
            
            if (idToPacket.TryGetValue(happenReason.Value.PacketNumber, out var pvm) && 
                (pvm.MainActor == null || !pvm.MainActor.Equals(happenReason.Value.MainActor)))
                relatedPacketsWithoutActors.Add(pvm.Id);
            
            if (happenReason.Value.AdditionalActors != null)
                foreach (var r in happenReason.Value.AdditionalActors)
                    relatedActors.Add(r);
            usedReasons.Add(happenReason.Value.PacketNumber);
            reasons.Enqueue(happenReason.Value.PacketNumber);
            while (reasons.Count != 0)
            {
                var reason = reasons.Dequeue();
                foreach (var action in processor.GetPossibleActionsForEvent(reason))
                {
                    var actionHappened = processor.GetAction(action.packetId)!;
                    bool isMovement = action.happened.Kind == EventType.Movement ||
                                      actionHappened.Value.Kind == ActionType.StartMovement;
                    if (!(isMovement && action.chance > 0.75 ||
                          !isMovement && action.chance > 0.55))
                        continue;

                    if (!usedReasons.Add(action.packetId))
                        continue;
                    
                    reasons.Enqueue(action.packetId);
                    if (actionHappened!.Value.MainActor != null)
                    {
                        if (relatedActors.Add(actionHappened.Value.MainActor.Value))
                        {
                            LOG.LogInformation("Taking {@actor} from packet {@packet} linked by {@reason}", actionHappened.Value.MainActor.ToWowParserString(), action.packetId, reason);
                        }
                    }

                    if (idToPacket.TryGetValue(action.packetId, out var packetViewModel) &&
                        (packetViewModel.MainActor == null ||
                         !packetViewModel.MainActor.Equals(actionHappened.Value.MainActor)))
                        relatedPacketsWithoutActors.Add(action.packetId);

                    if (actionHappened.Value.AdditionalActors != null)
                    {
                        foreach (var r in actionHappened.Value.AdditionalActors)
                        {
                            if (relatedActors.Add(r))
                            {
                                LOG.LogInformation(
                                    "Taking {@actor} from packet {@packet} (extra) linked by {@reason}", r.ToWowParserString(), action.packetId, reason);
                            }
                        }
                    }
                }
            }

            var filterData = new FilterData();
            var min = usedReasons.Min();
            var max = usedReasons.Max();
            filterData.SetMinMax(min, max);
            foreach (var actor in relatedActors)
            {
                filterData.IncludeGuid(actor);
            }
            foreach (var packet in relatedPacketsWithoutActors)
            {
                filterData.IncludePacketNumber(packet);
            }

            return filterData;
        }
    }
}