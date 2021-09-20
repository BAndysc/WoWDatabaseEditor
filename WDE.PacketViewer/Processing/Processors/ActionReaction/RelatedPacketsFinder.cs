using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        IFilterData Find(ICollection<PacketViewModel> packets, int start, CancellationToken token);
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
        
        public IFilterData Find(ICollection<PacketViewModel> packets, int start, CancellationToken token)
        {
            var processor = actionReactionProcessorCreator.Create();
            EventHappened? happenReason = null;

            foreach (var f in packets)
            {
                processor.PreProcess(f.Packet);
                if (token.IsCancellationRequested)
                    throw new TaskCanceledException();
            }

            foreach (var f in packets)
            {
                processor.Process(f.Packet);
                if (token.IsCancellationRequested)
                    throw new TaskCanceledException();
            }

            // Firstly we go backwards to find first ever possible event
            // This will be treated as a general reason of packet
            int pid = start;
            happenReason = null;
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

            Console.WriteLine("First event in chain: " + happenReason!.Value.Description + " (" + happenReason.Value.PacketNumber + ")");
            Queue<int> reasons = new Queue<int>();
            HashSet<int> usedReasons = new();
            List<int> relatedPacketsWithoutActors = new();
            HashSet<UniversalGuid> relatedActors = new HashSet<UniversalGuid>();
            if (happenReason.Value.MainActor != null)
                relatedActors.Add(happenReason.Value.MainActor);
            
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
                        if (relatedActors.Add(actionHappened.Value.MainActor))
                        {
                            Console.WriteLine("Taking " + actionHappened.Value.MainActor.ToWowParserString() +
                                              " from packet " + action.packetId + " linked by " + reason);
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
                                Console.WriteLine(
                                    "Taking " + r.ToWowParserString() +
                                    " form packet " + action.packetId + "(extra) linked by " + reason);
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