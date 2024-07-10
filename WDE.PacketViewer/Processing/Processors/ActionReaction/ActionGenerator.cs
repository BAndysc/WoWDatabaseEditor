using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.ActionReaction
{
    public enum ActionType
    {
        Chat,
        StartMovement,
        SpellCasted,
        Emote,
        AddGossip,
        ResetGossip,
        Summon,
        KillCredit,
        AddQuestGiverFlag,
        ResetQuestGiverFlag,
        ObjectDestroyed,
        ExitsCombat,
        EntersCombat,
        ServerQuestGiverCompleted,
        AuraRemoved,
        AuraApplied,
        ContinueMovement,
        GameObjectActivated,
        GossipSelect,
        GossipMessage,
        FactionChanged,
        EnableImmuneNpc,
        DisableImmuneNpc,
        EnableImmunePc,
        DisableImmunePc,
        CreateObjectInRange
    }
    
    public readonly struct ActionHappened
    {
        public DateTime Time { get; init; }
        public int PacketNumber { get; init; }
        public ActionType Kind { get; init; }
        public string Description { get; init; }
        public UniversalGuid? MainActor { get; init; }
        public List<UniversalGuid>? AdditionalActors { get; init; }
        public Vec3? EventLocation { get; init; }
        public int? CustomEntry { get; init; }
        public float? TimeFactor { get; init; }
        public EventType? RestrictEvent { get; init; }
    }
    
    public unsafe class ActionGenerator : PacketProcessor<IEnumerable<ActionHappened>?>
    {
        private readonly IDbcSpellService spellService;
        private readonly IUnitPositionFollower unitPosition;
        private readonly IChatEmoteSoundProcessor chatEmote;
        private readonly IUpdateObjectFollower updateObjectFollower;
        private readonly IPlayerGuidFollower playerGuidFollower;
        private readonly IWaypointProcessor waypointProcessor;
        private readonly IAuraSlotTracker auraSlotTracker;

        public ActionGenerator(
            IDbcSpellService spellService,
            IUnitPositionFollower unitPosition, 
            IChatEmoteSoundProcessor chatEmote,
            IUpdateObjectFollower updateObjectFollower,
            IPlayerGuidFollower playerGuidFollower,
            IWaypointProcessor waypointProcessor,
            IAuraSlotTracker auraSlotTracker)
        {
            this.spellService = spellService;
            this.unitPosition = unitPosition;
            this.chatEmote = chatEmote;
            this.updateObjectFollower = updateObjectFollower;
            this.playerGuidFollower = playerGuidFollower;
            this.waypointProcessor = waypointProcessor;
            this.auraSlotTracker = auraSlotTracker;
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
        
        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            var originalSpline = waypointProcessor.GetOriginalSpline(basePacket.Number);
            
            return new ActionHappened()
            {
                Kind = originalSpline.HasValue ? ActionType.ContinueMovement :  ActionType.StartMovement,
                PacketNumber = basePacket.Number,
                MainActor = packet.Mover,
                AdditionalActors = packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookTarget ? packet.LookTarget.Target.ToSingletonList() : null,
                Description = $"{packet.Mover.ToWowParserString()} " + (originalSpline.HasValue ? "continue" : "start") + " movement",
                EventLocation = unitPosition.GetPosition(packet.Mover, basePacket.Time.ToDateTime()),
                Time = basePacket.Time.ToDateTime(),
                CustomEntry = originalSpline
            }.ToSingletonList();
        }

        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            return new ActionHappened()
            {
                Time = basePacket.Time.ToDateTime(),
                PacketNumber = basePacket.Number,
                Kind = ActionType.Chat,
                Description = $"{packet.Sender.ToWowParserString()} says `{packet.Text}`",
                MainActor = packet.Sender,
                AdditionalActors = packet.Target.ToSingletonList(),
                EventLocation = unitPosition.GetPosition(packet.Sender, basePacket.Time.ToDateTime())
            }.ToSingletonList();
        }
        
        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverQuestComplete packet)
        {
            return new ActionHappened()
            {
                Kind = ActionType.ServerQuestGiverCompleted,
                PacketNumber = basePacket.Number,
                MainActor = playerGuidFollower.PlayerGuid,
                Description = $"Server confirms quest giver complete",
                EventLocation = unitPosition.GetPosition(playerGuidFollower.PlayerGuid ?? playerGuidFollower.PlayerGuid, basePacket.Time.ToDateTime()),
                Time = basePacket.Time.ToDateTime(),
                CustomEntry = (int?)packet.QuestId,
                RestrictEvent = EventType.PlayerPicksReward
            }.ToSingletonList();
        }

        protected override unsafe IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestAddKillCredit packet)
        {
            var victim = packet.Victim == null || packet.Victim->Type == UniversalHighGuid.Null ? null : packet.Victim;
            return new List<ActionHappened>()
            {
                new ActionHappened()
                {
                    Kind = ActionType.KillCredit,
                    PacketNumber = basePacket.Number,
                    MainActor = Unpack(victim),
                    AdditionalActors = playerGuidFollower.PlayerGuid.ToSingletonList(),
                    Description = $"Add kill credit " + packet.KillCredit,
                    EventLocation = unitPosition.GetPosition(packet.Victim == null || packet.Victim->Type == UniversalHighGuid.Null ? playerGuidFollower.PlayerGuid : Unpack(packet.Victim), basePacket.Time.ToDateTime()),
                    Time = basePacket.Time.ToDateTime(),
                    CustomEntry = (int?)packet.KillCredit
                }
            };
        }

        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet)
        {
            List<ActionHappened> output = new();
            foreach (ref readonly var update in packet.Updates.AsSpan())
            {
                var spellId = auraSlotTracker.GetSpellForAuraSlot(packet.Unit, update.Slot);
                if (!spellId.HasValue || !IsSpellImportant(spellId.Value))
                    continue;
                
                if (update.Remove)
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = ActionType.AuraRemoved,
                        PacketNumber = basePacket.Number,
                        MainActor = packet.Unit,
                        Description = $"Aura in slot {update.Slot} is removed from {packet.Unit.ToWowParserString()}",
                        EventLocation = unitPosition.GetPosition(packet.Unit, basePacket.Time.ToDateTime()),
                        CustomEntry = update.Slot,
                        Time = basePacket.Time.ToDateTime(),
                        RestrictEvent = EventType.AuraShouldBeRemoved
                    });
                }
                else
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = ActionType.AuraApplied,
                        PacketNumber = basePacket.Number,
                        MainActor = packet.Unit,
                        Description = $"Aura in slot {update.Slot} is added to {packet.Unit.ToWowParserString()}",
                        EventLocation = unitPosition.GetPosition(packet.Unit, basePacket.Time.ToDateTime()),
                        CustomEntry = (int)update.Spell,
                        Time = basePacket.Time.ToDateTime(),
                        RestrictEvent = EventType.SpellCasted
                    });
                }
            }

            return output;
        }

        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketGossipMessage packet)
        {
            return new ActionHappened()
            {
                Kind = ActionType.GossipMessage,
                PacketNumber = basePacket.Number,
                MainActor = packet.GossipSource,
                AdditionalActors = playerGuidFollower.PlayerGuid.ToSingletonList(),
                Description = $"Show gossip menu {packet.MenuId}",
                EventLocation = unitPosition.GetPosition(packet.GossipSource, basePacket.Time.ToDateTime()),
                Time = basePacket.Time.ToDateTime(),
                CustomEntry = (int)packet.MenuId
            }.ToSingletonList();
        }

        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketGossipSelect packet)
        {
            return new ActionHappened()
            {
                Kind = ActionType.GossipSelect,
                PacketNumber = basePacket.Number,
                MainActor = packet.GossipUnit,
                AdditionalActors = playerGuidFollower.PlayerGuid.ToSingletonList(),
                Description = $"Pick gossip option {packet.OptionId}",
                EventLocation = unitPosition.GetPosition(packet.GossipUnit, basePacket.Time.ToDateTime()),
                Time = basePacket.Time.ToDateTime(),
                CustomEntry = (int)packet.MenuId,
                RestrictEvent = EventType.GossipMessageShown
            }.ToSingletonList();
        }

        private uint? justUsedItemSpellID;
        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseItem packet)
        {
            justUsedItemSpellID = packet.SpellId;
            return base.Process(in basePacket, in packet);
        }

        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
        {
            List<ActionHappened> output = new();
            if (packet.Data == null)
                return output;
            if (!justUsedItemSpellID.HasValue ||
                justUsedItemSpellID.Value != packet.Data->Spell)
            {
                // skip boring spells, usually casted by a player
                if (!IsSpellImportant(packet.Data->Spell))
                    return output;

                if (!(spellService.GetAttributes<SpellAttr0>(packet.Data->Spell)
                          .HasFlagFast(SpellAttr0.DoNotDisplaySpellBookAuraIconCombatLog) ||
                      spellService.GetAttributes<SpellAttr0>(packet.Data->Spell)
                          .HasFlagFast(SpellAttr0.DoNotLog)))
                    return output;
            }

            return new ActionHappened()
            {
                Kind = ActionType.SpellCasted,
                PacketNumber = basePacket.Number,
                MainActor = packet.Data->Caster,
                AdditionalActors = packet.Data->TargetUnit.ToSingletonList(),
                Description = $"{packet.Data->Caster.ToWowParserString()} casts spell {packet.Data->Spell}",
                EventLocation = unitPosition.GetPosition(packet.Data->Caster, basePacket.Time.ToDateTime()),
                Time = basePacket.Time.ToDateTime(),
                CustomEntry = (int)packet.Data->Spell
            }.ToSingletonList();
        }

        protected override IEnumerable<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketEmote packet)
        {
            if (chatEmote.IsEmoteForChat(basePacket))
                return null;

            return new ActionHappened()
            {
                Kind = ActionType.Emote,
                PacketNumber = basePacket.Number,
                MainActor = packet.Sender,
                Description = $"{packet.Sender.ToWowParserString()} plays emote {packet.Emote}",
                EventLocation = unitPosition.GetPosition(packet.Sender, basePacket.Time.ToDateTime()),
                Time = basePacket.Time.ToDateTime()
            }.ToSingletonList();
        }

        protected override List<ActionHappened>? Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            List<ActionHappened> output = new();
            foreach (ref readonly var create in packet.Created.AsSpan())
            {
                if (create.CreateType == CreateObjectType.Spawn)
                {
                    if (create.Guid.Type != UniversalHighGuid.Creature &&
                        create.Guid.Type != UniversalHighGuid.GameObject &&
                        create.Guid.Type != UniversalHighGuid.Vehicle)
                        continue;
                    
                    bool _ = (!create.Values.TryGetGuid("UNIT_FIELD_DEMON_CREATOR", out var summoner) &&
                     !create.Values.TryGetGuid("UNIT_FIELD_SUMMONEDBY", out summoner) &&
                     !create.Values.TryGetGuid("UNIT_FIELD_CREATEDBY", out summoner));
                    output.Add(new ActionHappened()
                    {
                        Kind = ActionType.Summon,
                        PacketNumber = basePacket.Number,
                        MainActor = create.Guid,
                        AdditionalActors = summoner.ToSingletonList(),
                        Description = $"{create.Guid.ToWowParserString()} summoned",
                        EventLocation = unitPosition.GetPosition(create.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime(),
                        TimeFactor = 0.25f,
                        CustomEntry = create.Values.TryGetInt("UNIT_CREATED_BY_SPELL", out var spellSummon) ? (int)spellSummon : null
                    });
                }
                else
                {
                    if (create.Guid.Type != UniversalHighGuid.Creature &&
                         create.Guid.Type != UniversalHighGuid.GameObject &&
                         create.Guid.Type != UniversalHighGuid.Vehicle &&
                         create.Guid.Type != UniversalHighGuid.Player)
                        continue;
                    
                    output.Add(new ActionHappened()
                    {
                        Kind = ActionType.CreateObjectInRange,
                        PacketNumber = basePacket.Number,
                        MainActor = create.Guid,
                        Description = $"{create.Guid.ToWowParserString()} in range",
                        EventLocation = unitPosition.GetPosition(create.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime(),
                        RestrictEvent = EventType.TeleportUnit
                    });
                }
            }

            foreach (ref readonly var destroy in packet.Destroyed.AsSpan())
            {
                if (destroy.Guid.Type == UniversalHighGuid.Item || destroy.Guid.Type== UniversalHighGuid.DynamicObject)
                    continue;
                output.Add(new ActionHappened()
                {
                    Kind = ActionType.ObjectDestroyed,
                    PacketNumber = basePacket.Number,
                    MainActor = destroy.Guid,
                    Description = $"{destroy.Guid.ToWowParserString()} destroyed",
                    EventLocation = unitPosition.GetPosition(destroy.Guid, basePacket.Time.ToDateTime()),
                    Time = basePacket.Time.ToDateTime()
                });
            }
            
            foreach (ref readonly var update in packet.Updated.AsSpan())
            {
                if (FlagChanged(update, "UNIT_NPC_FLAGS", (long)GameDefines.NpcFlags.Gossip, out var added,
                    out var removed))
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = added ? ActionType.AddGossip : ActionType.ResetGossip,
                        PacketNumber = basePacket.Number,
                        MainActor = update.Guid,
                        Description = $"{update.Guid.ToWowParserString()} " + (added ? "adds gossip flag" : "resets gossip flag"),
                        EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime()
                    });
                }
                
                if (FlagChanged(update, "UNIT_NPC_FLAGS", (long)GameDefines.NpcFlags.QuestGiver, out var added2,
                    out var removed2))
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = added2 ? ActionType.AddQuestGiverFlag : ActionType.ResetQuestGiverFlag,
                        PacketNumber = basePacket.Number,
                        MainActor = update.Guid,
                        Description = $"{update.Guid.ToWowParserString()} " + (added2 ? "adds questgiver flag" : "resets questgiver flag"),
                        EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime()
                    });
                }
                
                if (FlagChanged(update, "UNIT_FIELD_FLAGS", (long)GameDefines.UnitFlags.InCombat, out var entersCombat,
                    out var exitsCombat))
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = entersCombat ? ActionType.EntersCombat : ActionType.ExitsCombat,
                        PacketNumber = basePacket.Number,
                        MainActor = update.Guid,
                        Description = $"{update.Guid.ToWowParserString()} " + (entersCombat ? "enters combat" : "exits combat"),
                        EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime(),
                        TimeFactor = exitsCombat ? 6f : 1f // exit combat action can only join with enters combat and we allow very far away enter combat
                    });
                }
                
                if (FlagChanged(update, "UNIT_FIELD_FLAGS", (long)GameDefines.UnitFlags.ImmuneToNpc, out var enableImmuneNpc,
                    out var disableImmuneNpc))
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = enableImmuneNpc ? ActionType.EnableImmuneNpc : ActionType.DisableImmuneNpc,
                        PacketNumber = basePacket.Number,
                        MainActor = update.Guid,
                        Description = $"{update.Guid.ToWowParserString()} " + (enableImmuneNpc ? " becomes immune to NPC" : " disables UNIT_FLAGS_IMMUNE_TO_NPC"),
                        EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime()
                    });
                }
                
                if (FlagChanged(update, "UNIT_FIELD_FLAGS", (long)GameDefines.UnitFlags.ImmuneToPc, out var enableImmunePc,
                    out var disableImmunePc))
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = enableImmunePc ? ActionType.EnableImmunePc : ActionType.DisableImmunePc,
                        PacketNumber = basePacket.Number,
                        MainActor = update.Guid,
                        Description = $"{update.Guid.ToWowParserString()} " + (enableImmuneNpc ? " becomes immune to PC" : " disables UNIT_FLAGS_IMMUNE_TO_PC"),
                        EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime()
                    });
                }

                if (FieldChanged(update, "UNIT_FIELD_FACTIONTEMPLATE", out var newFaction))
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = ActionType.FactionChanged,
                        PacketNumber = basePacket.Number,
                        MainActor = update.Guid,
                        Description = $"{update.Guid.ToWowParserString()} changes faction to {newFaction}",
                        EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime()
                    });
                }

                if (FieldChanged(update, "UNIT_NPC_EMOTESTATE", out var newValue) &&
                    update.Guid.Type != UniversalHighGuid.Player)
                {
                    output.Add(new ActionHappened()
                    {
                        Kind = ActionType.Emote,
                        PacketNumber = basePacket.Number,
                        MainActor = update.Guid,
                        Description = $"{update.Guid.ToWowParserString()} plays emotestate {newValue}",
                        EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                        Time = basePacket.Time.ToDateTime()
                    });
                }
                
                if (update.Values.TryGetInt("GAMEOBJECT_BYTES_1", out var bytes1)
                    && updateObjectFollower.TryGetInt(update.Guid, "GAMEOBJECT_BYTES_1", out var oldBytes))
                {
                    var oldState = oldBytes & 0xFF;
                    var newState = bytes1 & 0xFF;
                    if (oldState != newState)
                    {
                        output.Add(new ActionHappened()
                        {
                            Kind = ActionType.GameObjectActivated,
                            PacketNumber = basePacket.Number,
                            MainActor = update.Guid,
                            Description = $"GameObject {update.Guid.ToWowParserString()} activated/deactivated",
                            EventLocation = unitPosition.GetPosition(update.Guid, basePacket.Time.ToDateTime()),
                            Time = basePacket.Time.ToDateTime()
                        });
                    }
                }
            }

            return output;
        }

        private bool FieldChanged(UpdateObject update, string field, out long newValue)
        {
            if (update.Values.TryGetInt(field, out newValue) &&
                updateObjectFollower.TryGetInt(update.Guid, field, out var oldValue) &&
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
    }
}