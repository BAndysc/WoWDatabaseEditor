using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.ActionReaction
{
    internal static class Extension
    {
        public static List<UniversalGuid>? ToSingletonList(this UniversalGuid? guid)
        {
            if (guid == null)
                return null;

            return guid.Value.ToSingletonList();
        }

        public static List<UniversalGuid>? ToSingletonList(this UniversalGuid guid)
        {
            if (guid.Type == UniversalHighGuid.Null)
                return null;
            return new List<UniversalGuid>() { guid };
        }
    }

    public enum EventType
    {
        ChatOver,
        Spellclick,
        GossipSelect,
        QuestAccepted,
        QuestRewarded,
        Movement,
        SpellCasted,
        Emote,
        ClientAreaTrigger,
        Death,
        Activated,
        EnterCombat,
        ItemUsed,
        Spawned,
        GameObjectUsed,
        ExitCombat,
        PlayerPicksReward,
        StartChat,
        SummonBySpell,
        AuraRemoved,
        AuraShouldBeRemoved,
        StartMovement,
        OpenGameObjectBySpell,
        GossipMessageShown,
        EmoteState,
        GossipHello,
        LookAt,
        FinishingMovement,
        TeleportUnit
    }
        
    public readonly struct EventHappened
    {
        public DateTime Time { get; init; }
        public int PacketNumber { get; init; }
        public EventType Kind { get; init; }
        public string Description { get; init; }
        public UniversalGuid? MainActor { get; init; }
        public List<UniversalGuid>? AdditionalActors { get; init; }
        public Vec3? EventLocation { get; init; }
        public int? CustomEntry { get; init; }
        public ActionType? RestrictedAction { get; init; }
        public int? LimitActionsCount { get; init; }
        public TimeSpan? TimeCutOff { get; init; }
        /**
         * Determines how strong this event connects to time
         */
        public float? TimeFactor { get; init; }
    }
    
    public class EventDetectorProcessor : PacketProcessor<bool>
    {
        private readonly IDbcSpellService spellService;
        private readonly IUnitPositionFollower positionFollower;
        private readonly IRandomMovementDetector movementDetector;
        private readonly IChatEmoteSoundProcessor chatProcessor;
        private readonly IWaypointProcessor waypointsProcessor;
        private readonly IUpdateObjectFollower updateObjectFollower;
        private readonly IPlayerGuidFollower playerGuidFollower;
        private readonly IAuraSlotTracker auraSlotTracker;
        private readonly IDatabaseProvider databaseProvider;
        private List<EventHappened> eventsFeed = new();
        private List<EventHappened> futureEvents = new();

        public class EventBuilder
        {
            private readonly EventDetectorProcessor parent;
            private PacketBase packet;
            private EventType kind;
            private UniversalGuid? mainActor;
            private string? description;
            private List<UniversalGuid>? actors = null;
            private TimeSpan? offset;
            private Vec3? location;
            private int? customEntry;
            private float? timeFactor;
            private ActionType? restrictedAction;
            private int? limitActionsCount;
            private TimeSpan? cutOff;
            
            public EventBuilder(EventDetectorProcessor parent, PacketBase packet, EventType kind)
            {
                this.parent = parent;
                this.packet = packet;
                this.kind = kind;
            }

            public EventBuilder SetDescription(string description)
            {
                this.description = description;
                return this;
            }

            public EventBuilder InFuture(TimeSpan offset)
            {
                this.offset = offset;
                return this;
            }

            public EventBuilder AddActors(IEnumerable<UniversalGuid?> actors)
            {
                foreach (var a in actors)
                    AddActor(a);
                return this;
            }

            public EventBuilder AddActors(ReadOnlySpan<UniversalGuid> actors)
            {
                foreach (var a in actors)
                    AddActor(a);
                return this;
            }
            
            public EventBuilder AddActor(UniversalGuid? actor)
            {
                if (!actor.HasValue)
                    return this;

                if (mainActor == null)
                    mainActor = actor;
                else
                {
                    if (actors == null)
                        actors = new List<UniversalGuid>();
                    actors.Add(actor.Value);
                }
                
                return this;
            }

            public EventBuilder SetTimeFactor(float time)
            {
                timeFactor = time;
                return this;
            }
            
            public EventBuilder SetLocation(Vec3? location)
            {
                if (location == null)
                    return this;

                this.location = location;
                
                return this;
            }
            
            public EventBuilder RestrictAction(ActionType restrictedAction)
            {
                this.restrictedAction = restrictedAction;
                return this;
            }
            
            public EventBuilder LimitActions(int count)
            {
                this.limitActionsCount = count;
                return this;
            }
            
            public EventBuilder SetCustomEntry(int? entry)
            {
                if (!entry.HasValue)
                    return this;
                customEntry = entry;
                return this;
            }

            public EventBuilder TimeCutOff(TimeSpan span)
            {
                this.cutOff = span;
                return this;
            }
            
            public void Push()
            {
                var eh = new EventHappened()
                {
                    Time = packet.Time.ToDateTime() + (offset ?? TimeSpan.Zero),
                    Kind = kind,
                    PacketNumber = packet.Number,
                    MainActor = mainActor,
                    AdditionalActors = actors,
                    Description = description ?? "?",
                    EventLocation = location ?? parent.positionFollower.GetPosition(mainActor, packet.Time.ToDateTime()),
                    CustomEntry = customEntry,
                    TimeFactor = timeFactor,
                    RestrictedAction = restrictedAction,
                    LimitActionsCount = limitActionsCount,
                    TimeCutOff = cutOff
                };
                if (offset.HasValue)
                {
                    int index = 0;
                    while (parent.futureEvents.Count > index && parent.futureEvents[index].Time < eh.Time)
                        index++;
                    parent.futureEvents.Insert(index, eh);
                }
                else
                {
                    parent.Flush(eh.Time);
                    parent.eventsFeed.Add(eh);
                }
            }
        }

        public int CurrentIndex => eventsFeed.Count;

        public EventBuilder Event(PacketBase packet, EventType kind)
        {
            return new EventBuilder(this, packet, kind);
        }
        
        public EventDetectorProcessor(
            IDbcSpellService spellService,
            IUnitPositionFollower positionFollower,
            IRandomMovementDetector movementDetector, 
            IChatEmoteSoundProcessor chatProcessor,
            IWaypointProcessor waypointsProcessor,
            IUpdateObjectFollower updateObjectFollower,
            IPlayerGuidFollower playerGuidFollower,
            IAuraSlotTracker auraSlotTracker,
            IDatabaseProvider databaseProvider)
        {
            this.spellService = spellService;
            this.positionFollower = positionFollower;
            this.movementDetector = movementDetector;
            this.chatProcessor = chatProcessor;
            this.waypointsProcessor = waypointsProcessor;
            this.updateObjectFollower = updateObjectFollower;
            this.playerGuidFollower = playerGuidFollower;
            this.auraSlotTracker = auraSlotTracker;
            this.databaseProvider = databaseProvider;
        }
        
        public void Flush(DateTime time)
        {
            while (futureEvents.Count > 0 && futureEvents[0].Time < time)
            {
                eventsFeed.Add(futureEvents[0]);
                futureEvents.RemoveAt(0);
            }
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellClick packet)
        {
            Event(basePacket, EventType.Spellclick)
                .SetDescription("on spellclick")
                .AddActor(packet.Target)
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetTimeFactor(0.2f)
                .RestrictAction(ActionType.SpellCasted)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet)
        {
            bool anyRemoved = false;
            foreach (ref readonly var aura in packet.Updates.AsSpan())
            {
                var spellId = auraSlotTracker.GetSpellForAuraSlot(packet.Unit, aura.Slot);
                if (!spellId.HasValue || !IsSpellImportant(spellId.Value))
                    continue;
                
                if (aura.Remove)
                {
                    anyRemoved = true;
                }
                else if (aura.Remaining.HasValue)
                {
                    Event(basePacket, EventType.AuraShouldBeRemoved)
                        .SetDescription($"Aura {aura.Spell} should be removed according to duration from {packet.Unit.ToWowParserString()}")
                        .AddActor(packet.Unit)
                        .InFuture(TimeSpan.FromMilliseconds(aura.Remaining.Value))
                        .SetTimeFactor(0.1f)
                        .SetCustomEntry(aura.Slot)
                        .RestrictAction(ActionType.AuraRemoved)
                        .Push();
                }
            }

            if (anyRemoved)
            {
                Event(basePacket, EventType.AuraRemoved)
                    .SetDescription("On aura removed from " + packet.Unit.ToWowParserString())
                    .AddActor(packet.Unit)
                    .SetTimeFactor(0.6f)
                    .Push();
            }
            return base.Process(in basePacket, in packet);
        }

        private bool IsSpellImportant(uint spellId)
        {
            if (!spellService.Exists(spellId))
                return false;

            if (spellService.GetSpellFocus(spellId).HasValue) // spells with spell focus seems interesting
                return true;

            if (spellService.GetSkillLine(spellId) == 777) // Mounts
                return false;
            
            if (spellService.GetDescription(spellId) == null) // empty description means something interesting
                return true;
            
            if (!spellService.Exists(spellId) ||
                !spellService.GetAttributes<SpellAttr0>(spellId).HasFlagFast(SpellAttr0.DoNotDisplaySpellBookAuraIconCombatLog))
                return false;

            return true;
        }

        protected override unsafe bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
        {
            if (packet.Data == null)
                return false;

            var spellId = packet.Data->Spell;
            if (!IsSpellImportant(spellId))
                return false;

            var effects = spellService.GetSpellEffectsCount(spellId);
            for (int index = 0; index < effects; ++index)
            {
                var effectType = spellService.GetSpellEffectType(spellId, index);
                if (effectType == SpellEffectType.Summon)
                {
                    Event(basePacket, EventType.SummonBySpell)
                        .SetDescription("On effect SUMMON of spell " + packet.Data->Spell + " casted by " + packet.Data->Caster.ToWowParserString())
                        .AddActor(packet.Data->Caster)
                        .AddActor(packet.Data->TargetUnit)
                        .AddActors(packet.Data->HitTargets.AsSpan())
                        .SetTimeFactor(0.2f)
                        .SetCustomEntry((int)spellService.GetSpellEffectMiscValueA(spellId, index))
                        .Push();
                }
                else if (effectType == SpellEffectType.OpenLock)
                {
                    Event(basePacket, EventType.OpenGameObjectBySpell)
                        .SetDescription("On effect OPEN_LOCK of spell " + packet.Data->Spell + " casted by " + packet.Data->Caster.ToWowParserString())
                        .AddActor(packet.Data->TargetUnit)
                        .AddActor(packet.Data->Caster)
                        .AddActors(packet.Data->HitTargets)
                        .SetTimeFactor(0.2f)
                        .RestrictAction(ActionType.GameObjectActivated)
                        .Push();
                }
                else if (effectType == SpellEffectType.TeleportUnits)
                {
                    Event(basePacket, EventType.TeleportUnit)
                        .SetDescription("On effect TeleportUnits of spell " + packet.Data->Spell + " casted by " + packet.Data->Caster.ToWowParserString())
                        .AddActors(packet.Data->HitTargets)
                        .SetLocation(Unpack(packet.Data->DstLocation))
                        .RestrictAction(ActionType.CreateObjectInRange)
                        .SetTimeFactor(2)
                        .Push();
                }
            }

            Event(basePacket, EventType.SpellCasted)
                .SetDescription("On spell " + packet.Data->Spell + " casted by " + packet.Data->Caster.ToWowParserString())
                .AddActor(packet.Data->Caster)
                .AddActor(packet.Data->TargetUnit)
                .AddActors(packet.Data->HitTargets)
                .SetTimeFactor(0.25f)
                .SetCustomEntry((int)packet.Data->Spell)
                .Push();
            return base.Process(in basePacket, in packet);
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipMessage packet)
        {
            Event(basePacket, EventType.GossipMessageShown)
                .SetDescription("Gossip message shown")
                .AddActor(packet.GossipSource)
                .AddActor(playerGuidFollower.PlayerGuid)
                .RestrictAction(ActionType.GossipSelect)
                .SetCustomEntry((int)packet.MenuId)
                .LimitActions(1)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipHello packet)
        {
            Event(basePacket, EventType.GossipHello)
                .SetDescription("Gossip hello")
                .AddActor(packet.GossipSource)
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetTimeFactor(0.5f)
                .RestrictAction(ActionType.GossipMessage)
                .LimitActions(1)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipSelect packet)
        {
            Event(basePacket, EventType.GossipSelect)
                .SetDescription("Gossip selected")
                .AddActor(packet.GossipUnit)
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetTimeFactor(0.2f)
                .TimeCutOff(TimeSpan.FromSeconds(4.5f))
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverAcceptQuest packet)
        {
            Event(basePacket, EventType.QuestAccepted)
                .SetDescription($"Quest {packet.QuestId} accepted")
                .AddActor(packet.QuestGiver)
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetTimeFactor(0.4f)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        private PacketClientQuestGiverChooseReward? lastChooseReward;
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientQuestGiverChooseReward packet)
        {
            lastChooseReward = packet;
            Event(basePacket, EventType.PlayerPicksReward)
                .SetDescription($"Player picks {packet.QuestId} reward")
                .AddActor(packet.QuestGiver)
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetTimeFactor(0.2f)
                .RestrictAction(ActionType.ServerQuestGiverCompleted)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverQuestComplete packet)
        {
            Event(basePacket, EventType.QuestRewarded)
                .SetDescription($"Quest {packet.QuestId} rewarded")
                .AddActor(lastChooseReward?.QuestId == packet.QuestId ? lastChooseReward?.QuestGiver : null)
                .AddActor(playerGuidFollower.PlayerGuid)
                //.SetTimeFactor(0.4f)
                .TimeCutOff(TimeSpan.FromSeconds(9))
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseGameObject packet)
        {
            Event(basePacket, EventType.GameObjectUsed)
                .SetDescription($"Object {packet.GameObject.ToWowParserString()} used")
                .AddActor(packet.GameObject)
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetTimeFactor(0.2f)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            //if (movementDetector.IsRandomMovementPacket(basePacket))
            //    return false;

            if (movementDetector.RandomMovementPacketRatio(packet.Mover) > 0.55f)
                return false;
            
            var originalSpline = waypointsProcessor.GetOriginalSpline(basePacket.Number);
            
            if (packet.Points.Count > 0)
            {
                Event(basePacket, EventType.StartMovement)
                    .SetDescription($"{packet.Mover.ToWowParserString()} starts movement")
                    .AddActor(packet.Mover)
                    .SetLocation(packet.Position)
                    .SetCustomEntry(originalSpline ?? basePacket.Number)
                    .RestrictAction(ActionType.ContinueMovement)
                    .Push();
                
                Event(basePacket, EventType.Movement)
                    .InFuture(TimeSpan.FromMilliseconds(packet.MoveTime))
                    .SetDescription($"{packet.Mover.ToWowParserString()} finshed movement")
                    .AddActor(packet.Mover)
                    .SetLocation(packet.Points[^1])
                    .Push();
                
                Event(basePacket, EventType.FinishingMovement)
                    .InFuture(TimeSpan.FromMilliseconds(packet.MoveTime * 0.9f))
                    .SetDescription($"{packet.Mover.ToWowParserString()} is approaching to finishing movement (90% done)")
                    .AddActor(packet.Mover)
                    .SetCustomEntry(originalSpline ?? basePacket.Number)
                    .SetLocation(packet.Points[^1])
                    .RestrictAction(ActionType.ContinueMovement)
                    .Push();
            }
            else if (packet.FacingCase != PacketMonsterMove.FacingOneofCase.None)
            {
                string? desc = null;
                if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookPosition)
                    desc = $"looks at {packet.LookPosition.X}, {packet.LookPosition.Y}, {packet.LookPosition.Z}";
                else if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookTarget)
                    desc = $"looks at {packet.LookTarget.Target.ToWowParserString()}";
                else
                    desc = $"sets orientation to {packet.LookOrientation}";
                Event(basePacket, EventType.LookAt)
                    .SetDescription($"{packet.Mover.ToWowParserString()} {desc}")
                    .AddActor(packet.Mover)
                    .AddActor(packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookTarget ? packet.LookTarget.Target : null)
                    .SetLocation(packet.Position)
                    .Push();
            }
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            Event(basePacket, EventType.StartChat)
                .AddActor(packet.Sender)
                .AddActor(packet.Target)
                .SetDescription($"Text {packet.Text} by {packet.Sender.ToWowParserString()} start")
                .SetTimeFactor(0.6f)
                .Push();
            Event(basePacket, EventType.ChatOver)
                .InFuture(TimeSpan.FromSeconds(3))
                .AddActor(packet.Sender)
                .AddActor(packet.Target)
                .SetDescription($"Text {packet.Text} by {packet.Sender.ToWowParserString()} over")
                .SetTimeFactor(0.6f)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientAreaTrigger packet)
        {
            Event(basePacket, EventType.ClientAreaTrigger)
                .SetDescription("Areatrigger")
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetTimeFactor(0.3f)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseItem packet)
        {
            Event(basePacket, EventType.ItemUsed)
                .SetDescription("Item used, spell " + packet.SpellId + " casted")
                .AddActor(playerGuidFollower.PlayerGuid)
                .SetCustomEntry((int)packet.SpellId)
                .SetTimeFactor(0.2f)
                .Push();
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            foreach (ref readonly var create in packet.Created.AsSpan())
            {
                if (create.Guid.Type != UniversalHighGuid.Creature &&
                    create.Guid.Type != UniversalHighGuid.GameObject &&
                    create.Guid.Type != UniversalHighGuid.Vehicle)
                    continue;
                
                if (create.CreateType != CreateObjectType.Spawn)
                    continue;

                #if VERIFY_IF_SPAWN_EXIST
                if (create.Guid.Type != UniversalHighGuid.GameObject)
                {
                    var creaturesInDb = databaseProvider.GetCreaturesByEntry(create.Guid.Entry);
                    if (creaturesInDb.Any(c => Dist((c.X, c.Y, c.Z),
                        (create.Movement.Position.X, create.Movement.Position.Y, create.Movement.Position.Z)) < 5))
                        continue;
                }
                #endif
                
                create.Values.TryGetGuid("UNIT_FIELD_DEMON_CREATOR", out var summoner);
                Event(basePacket, EventType.Spawned)
                    .AddActor(create.Guid)
                    .AddActor(summoner)
                    .SetDescription($"{create.Guid.ToWowParserString()} summoned")
                    .Push();
            }
            
            foreach (ref readonly var update in packet.Updated.AsSpan())
            {
                if (update.Values.TryGetInt("UNIT_FIELD_HEALTH", out var hp) && hp == 0)
                {
                    Event(basePacket, EventType.Death)
                        .SetDescription($"Unit {update.Guid.ToWowParserString()} dies")
                        .AddActor(update.Guid)
                        .Push();
                }
                
                if (FieldChanged(update, "UNIT_NPC_EMOTESTATE", out var newValue) &&
                    update.Guid.Type != UniversalHighGuid.Player)
                {
                    Event(basePacket, EventType.EmoteState)
                        .SetDescription($"Unit {update.Guid.ToWowParserString()} changes emote state to " + newValue)
                        .AddActor(update.Guid)
                        .Push();
                }
                
                if (FlagChanged(update, "UNIT_FIELD_FLAGS", (long)GameDefines.UnitFlags.InCombat, out var added, out var removed))
                {
                    if (added)
                    {
                        Event(basePacket, EventType.EnterCombat)
                            .SetDescription($"Unit {update.Guid.ToWowParserString()} enters combat")
                            .AddActor(update.Guid)
                            .Push();
                    }
                    else if (removed)
                    {
                        Event(basePacket, EventType.ExitCombat)
                            .SetDescription($"Unit {update.Guid.ToWowParserString()} exits combat")
                            .AddActor(update.Guid)
                            .Push();
                    }
                }
                

                if (update.Values.TryGetInt("GAMEOBJECT_BYTES_1", out var bytes1)
                    && updateObjectFollower.TryGetInt(update.Guid, "GAMEOBJECT_BYTES_1", out var oldBytes))
                {
                    var oldState = oldBytes & 0xFF;
                    var newState = bytes1 & 0xFF;
                    if (oldState != newState)
                    {
                        Event(basePacket, EventType.Activated)
                            .SetDescription($"Gameobject {update.Guid.ToWowParserString()} activated/deactivated")
                            .AddActor(update.Guid)
                            .Push();
                    }
                }
            }
            return base.Process(in basePacket, in packet);
        }

        private float Dist((float X, float Y, float Z) a, (float X, float Y, float Z) b)
        {
            return (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) +
                                    (a.Y - b.Y) * (a.Y - b.Y) +
                                    (a.Z - b.Z) * (a.Z - b.Z));
        }

        private bool FieldChanged(UpdateObject update, string field, out long newValue)
        {
            if (update.Values.TryGetInt(field, out newValue) &&
                updateObjectFollower.TryGetIntOrDefault(update.Guid, field, out var oldValue) &&
                newValue != oldValue)
                return true;

            newValue = 0;
            return false;
        }
        
        private bool FlagChanged(UpdateObject update, string field, long flag, out bool added, out bool removed)
        {
            if (update.Values.TryGetInt(field, out var current) &&
                updateObjectFollower.TryGetIntOrDefault(update.Guid, field, out var oldValue) &&
                current != oldValue &&
                (current & flag) != (oldValue & flag))
            {
                added = (oldValue & flag) == 0;
                removed = (oldValue & flag) == flag;
                return true;
            }

            added = false;
            removed = false;
            return false;
        }
        
        public EventHappened GetEvent(int i)
        {
            return eventsFeed[i];
        }
    }
}