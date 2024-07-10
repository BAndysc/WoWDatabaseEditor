using System;
using System.Collections.Generic;
using System.Linq;
using WDE.PacketViewer.Processing.Processors.ActionReaction.Rating;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.ActionReaction
{
    public class ActionReactionProcessor : IPacketProcessor<bool>, ITwoStepPacketBoolProcessor, IUnfilteredPacketProcessor
    {
        private readonly ActionGenerator actionGenerator;
        private readonly EventDetectorProcessor eventDetectorProcessor;
        private readonly IChatEmoteSoundProcessor chatEmoteSoundProcessor;
        private readonly IWaypointProcessor waypointsProcessor;
        private readonly IUnitPositionFollower unitPositionFollower;
        private readonly IUpdateObjectFollower updateObjectFollower;
        private readonly IPlayerGuidFollower playerGuidFollower;
        private readonly IAuraSlotTracker auraSlotTracker;

        public ActionReactionProcessor(
            ActionGenerator actionGenerator,
            EventDetectorProcessor eventDetectorProcessor,
            IChatEmoteSoundProcessor chatEmoteSoundProcessor, 
            IWaypointProcessor waypointsProcessor,
            IUnitPositionFollower unitPositionFollower,
            IUpdateObjectFollower updateObjectFollower,
            IPlayerGuidFollower playerGuidFollower,
            IAuraSlotTracker auraSlotTracker)
        {
            this.actionGenerator = actionGenerator;
            this.eventDetectorProcessor = eventDetectorProcessor;
            this.chatEmoteSoundProcessor = chatEmoteSoundProcessor;
            this.waypointsProcessor = waypointsProcessor;
            this.unitPositionFollower = unitPositionFollower;
            this.updateObjectFollower = updateObjectFollower;
            this.playerGuidFollower = playerGuidFollower;
            this.auraSlotTracker = auraSlotTracker;
        }

        public EventHappened? GetLastEventHappened()
        {
            if (eventDetectorProcessor.CurrentIndex == -1)
                return null;
            return eventDetectorProcessor.GetEvent(eventDetectorProcessor.CurrentIndex);
        }
        
        public IEnumerable<ActionHappened>? GetAllActions(int number)
        {
            if (!actionsHappened.TryGetValue(number, out var state))
                return null;
            return state.Item2;
        }
        
        public IEnumerable<EventHappened>? GetAllEvents(int number)
        {
            BuildReverseLookup();
            if (!eventsHappened.TryGetValue(number, out var state))
                return null;
            return state;
        }

        public ActionHappened? GetAction(int number)
        {
            if (!actionsHappened.TryGetValue(number, out var state))
                return null;
            return state.Item2[0];
        }

        public ActionHappened? GetAction(PacketBase packet) => GetAction(packet.Number);

        public IEnumerable<(FinalEvaluation rate, EventHappened @event)> GetPossibleEventsForAction(PacketBase packet) => GetPossibleEventsForAction(packet.Number);

        public IEnumerable<(FinalEvaluation rate, EventHappened @event)> GetPossibleEventsForAction(int number)
        {
            if (chatEmoteSoundProcessor.IsEmoteForChat(number))
            {
                yield return (FinalEvaluation.From(Const.One), new EventHappened()
                {
                    Description = "Emote for the next SMSG_CHAT",
                    PacketNumber = chatEmoteSoundProcessor.GetChatPacketForEmote(number)!.Value
                });
                yield break;
            }

            // if (waypointsProcessor.GetOriginalSpline(number) is { } originalPacket)
            // {
            //     yield return (Const.One, new EventHappened()
            //     {
            //         Description = $"Part of spline begun in packet {originalPacket}",
            //         PacketNumber = originalPacket
            //     });
            //     yield break;
            // }
            
            if (!actionsHappened.TryGetValue(number, out var state))
                yield break;

            var action = state.Item2[0];
            
            List<(FinalEvaluation, EventHappened)> reasons = new();
            int i = state.Item1;
            while (i >= 0 && reasons.Count < 50)
            {
                var @event = eventDetectorProcessor.GetEvent(i--);

                if (@event.PacketNumber == action.PacketNumber)
                    continue;
                
                float? distToEvent = null;
                if (@event.EventLocation != null && action.EventLocation != null)
                    distToEvent = @event.EventLocation.Distance3D(action.EventLocation);

                var timePassed = action.Time - @event.Time;

                if (timePassed.TotalSeconds > 20 || (@event.TimeCutOff.HasValue && timePassed > @event.TimeCutOff.Value))
                    break;
                
                // special casesF

                
                if (@event.RestrictedAction.HasValue &&
                    @event.RestrictedAction.Value != action.Kind)
                    continue;
                
                if (action.RestrictEvent.HasValue &&
                    action.RestrictEvent.Value != @event.Kind)
                    continue;

                if (action.Kind == ActionType.GossipMessage &&
                    (@event.Kind != EventType.GossipSelect &&
                     @event.Kind != EventType.GossipHello))
                    continue;
                
                if (@event.Kind == EventType.AuraShouldBeRemoved &&
                    action.Kind == ActionType.AuraRemoved &&
                    @event.CustomEntry != action.CustomEntry)
                    continue;

                if (@event.Kind == EventType.GossipMessageShown &&
                    action.Kind == ActionType.GossipSelect &&
                    @event.CustomEntry != action.CustomEntry)
                    continue;
                
                // special cases

                if (action.Kind == ActionType.ContinueMovement)
                {
                    if (@event.Kind != EventType.StartMovement && @event.Kind != EventType.FinishingMovement)
                        continue;
                    
                    if (@event.CustomEntry != action.CustomEntry)
                        continue;
                }

                //

                if (action.Kind == ActionType.CreateObjectInRange &&
                    @event.Kind == EventType.TeleportUnit)
                {
                    var dist = @event.EventLocation!.Distance3D(action.EventLocation!);
                    if (dist > 4 || action.MainActor == null)
                        continue;
                    if (!action.MainActor.Equals(@event.MainActor) &&
                        !(@event.AdditionalActors != null && @event.AdditionalActors.Contains(action.MainActor.Value)))
                        continue;
                }
                
                //
                
                if (@event.Kind == EventType.SpellCasted &&
                    action.Kind == ActionType.AuraApplied &&
                    @event.CustomEntry != action.CustomEntry)
                    continue;
                
                //
                
                if (@event.Kind == EventType.SummonBySpell &&
                    (action.Kind != ActionType.Summon || action.MainActor!.Value.Entry != @event.CustomEntry))
                    continue;

                if (action.Kind == ActionType.ExitsCombat && (
                    @event.Kind != EventType.EnterCombat ||
                    !action.MainActor!.Equals(@event.MainActor)))
                    continue;
                
                if (action.Kind == ActionType.SpellCasted &&
                    @event.Kind == EventType.ItemUsed &&
                    action.CustomEntry != @event.CustomEntry)
                    continue;

                //
                var mainActorMatches = action.MainActor?.Equals(@event.MainActor) ?? false;
                var actorsRating = mainActorMatches ? Const.One : Const.Zero;
                if (actorsRating.Value == 0 && action.AdditionalActors != null &&
                    action.AdditionalActors[0].Equals(@event.MainActor))
                    actorsRating = Const.One;
                else if (actorsRating.Value == 0 && @event.AdditionalActors != null &&
                         @event.AdditionalActors[0].Equals(action.MainActor))
                    actorsRating = Const.One;
                else if (actorsRating.Value == 0 && action.AdditionalActors != null &&
                         @event.AdditionalActors != null &&
                         @event.AdditionalActors[0].Equals(action.AdditionalActors[0]))
                    actorsRating = Const.Half;
                if (actorsRating.Value == 0 && action.CustomEntry.HasValue &&
                    action.CustomEntry.Value == @event.MainActor?.Entry)
                    actorsRating = new Const(0.8f);

                OneMinus distRating = OneMinus.From(Power.From(Remap.From(new Const(distToEvent ?? 33.5f), 0, 75, 0, 1), 1));

                OneMinus timeRating = OneMinus.From(Power.From(Remap.From(new Const((float)timePassed.TotalMilliseconds), 0, 10000 * (@event.TimeFactor ?? 1) * (action.TimeFactor ?? 1), 0, 1),  4));

                Const bonus = action.CustomEntry.HasValue && @event.CustomEntry.HasValue &&
                               action.CustomEntry.Value == @event.CustomEntry.Value ? Const.One : Const.Zero;
                
                var rating = Weighted.From((actorsRating, 0.4f), (timeRating, 0.35f), (distRating, 0.25f), (bonus, 0.2f));

                float bonusMult = (@event.Kind == EventType.ChatOver && action.Kind == ActionType.Chat) ? 1.3f : 1.0f;

                // bonus for packets sent in the same time
                if (@event.Time == action.Time)
                    bonusMult += 0.5f;
                
                if (@event.Kind == EventType.SummonBySpell &&
                    action.Kind == ActionType.Summon && action.MainActor!.Value.Entry == @event.CustomEntry)
                    bonusMult += 10;
                
                if (action.Kind == ActionType.CreateObjectInRange &&
                    @event.Kind == EventType.TeleportUnit)
                    bonusMult += 2;
                
                if (@event.Kind == EventType.Spellclick &&
                    action.Kind == ActionType.SpellCasted)
                    bonusMult += 0.5f;

                if (@event.Kind == EventType.GossipSelect &&
                    action.Kind == ActionType.GossipMessage &&
                    timePassed.TotalSeconds > 1)
                    bonusMult = 0; // if event is select option, then action gossip message is accepted only if it is within ~0.5s

                if (@event.Kind == EventType.Movement && distRating.Value < 0.01)
                    bonusMult = 0;

                // we are giving a bonus if the thing happened by exactly the same actor, within last 4 seconds
                if (mainActorMatches)
                    bonusMult += 0.2f * (float)(1 - Math.Clamp(timePassed.TotalSeconds / 4, 0, 1));
                
                var ratingBonused = Multiply.From(rating, bonusMult);

                if (ratingBonused.Value > 0.5f || ratingBonused.Value > 0.25f && actorsRating.Value > 0.5f)
                    reasons.Add((FinalEvaluation.From(ratingBonused), @event));
            }

            if (reasons.Count > 0)
            {
                foreach (var r in reasons.OrderByDescending(o => o.Item1.Value))
                {
                    yield return r;
                }
            }
        }
        
        // action packet Id -> what are actions
        private Dictionary<int, (int, List<ActionHappened>)> actionsHappened = new();
        
        // event packet Id -> what are events
        private Dictionary<int, List<EventHappened>> eventsHappened = new();
        
        // reverse lookup: event packet id -> what are possible actions, this event caused
        private Dictionary<int, List<(int packetId, double chance, EventHappened happened)>>? reverseLookup;

        public void Initialize(ulong gameBuild)
        {
            chatEmoteSoundProcessor.Initialize(gameBuild);
            waypointsProcessor.Initialize(gameBuild);
            auraSlotTracker.Initialize(gameBuild);
            playerGuidFollower.Initialize(gameBuild);
            unitPositionFollower.Initialize(gameBuild);
            updateObjectFollower.Initialize(gameBuild);
            eventDetectorProcessor.Initialize(gameBuild);
        }
        
        public void ProcessUnfiltered(ref PacketHolder packet)
        {
            auraSlotTracker.Process(in packet);
            playerGuidFollower.Process(in packet);
            unitPositionFollower.Process(in packet);
            updateObjectFollower.Process(in packet);
        }
        
        public virtual bool Process(ref readonly PacketHolder packet)
        {
            auraSlotTracker.Process(in packet);
            playerGuidFollower.Process(in packet);
            unitPositionFollower.Process(in packet);
            
            eventDetectorProcessor.Flush(packet.BaseData.Time.ToDateTime());
            var actions = actionGenerator.Process(in packet);
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    if (!actionsHappened.TryGetValue(packet.BaseData.Number, out var list))
                        list = actionsHappened[packet.BaseData.Number] = (eventDetectorProcessor.CurrentIndex - 1,
                            new List<ActionHappened>());
                    
                    list.Item2.Add(action);
                }
            }

            eventDetectorProcessor.Process(in packet);

            updateObjectFollower.Process(in packet);
            return true;
        }

        public void BuildReverseLookup()
        {
            if (reverseLookup != null)
                return;
            reverseLookup = new();
            
            foreach (var actionPacketId in actionsHappened)
            {
                foreach (var reason in GetPossibleEventsForAction(actionPacketId.Key))
                {
                    if (!reverseLookup.TryGetValue(reason.@event.PacketNumber, out var r))
                        r = reverseLookup[reason.@event.PacketNumber] = new();
                    r.Add((actionPacketId.Key, reason.rate.Value, reason.@event));
                }
            }

            for (int i = 0; i < eventDetectorProcessor.CurrentIndex; ++i)
            {
                var e = eventDetectorProcessor.GetEvent(i);
                if (!eventsHappened.TryGetValue(e.PacketNumber, out var list))
                    list = eventsHappened[e.PacketNumber] = new();
                list.Add(e);
            }
        }

        // returns sorted list of possible actions by event packet id
        public IEnumerable<(int packetId, double chance, EventHappened happened)> GetPossibleActionsForEvent(int eventPacketId)
        {
            BuildReverseLookup();
            if (reverseLookup!.TryGetValue(eventPacketId, out var list))
            {
                if (list.Count == 0)
                    return list;
                var limit = list[0].happened.LimitActionsCount;
                IEnumerable<(int packetId, double chance, EventHappened happened)> enumerable = list.OrderByDescending(p => p.chance);
                if (limit.HasValue)
                    enumerable = enumerable.Take(limit.Value);
                return enumerable;
            }
            return Enumerable.Empty<(int packetId, double chance, EventHappened happened)>();
        }

        public bool PreProcess(ref readonly PacketHolder packet)
        {
            chatEmoteSoundProcessor.Process(in packet);
            waypointsProcessor.Process(in packet);
            return true;
        }
    }
}