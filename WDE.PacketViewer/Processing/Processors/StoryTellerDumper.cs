using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors.Utils;
using WowPacketParser.Proto;
using WDE.PacketViewer.Processing.Runners;
using WDE.PacketViewer.Utils;
using DynamicData;

namespace WDE.PacketViewer.Processing.Processors
{
    [AutoRegister]
    public unsafe class StoryTellerDumper : CompoundProcessor<bool, IWaypointProcessor, IChatEmoteSoundProcessor, IRandomMovementDetector, IDespawnDetector, ISpellCastProcessor, IAuraSlotTracker>,
        IPacketTextDumper, ITwoStepPacketBoolProcessor, IUnfilteredPacketProcessor, IUnfilteredTwoStepPacketBoolProcessor
    {
        private class WriterBuilder
        {
            public static WriterBuilder Null { get; } = new(true);
            public DateTime? lastTime;
            public readonly StringBuilder builder;
            public readonly TextWriter writer;

            private WriterBuilder(bool @null)
            {
                builder = null!;
                writer = TextWriter.Null;
            }
            
            public WriterBuilder()
            {
                builder = new();
                writer = new StringWriter(builder);
            }
        }
        
        private readonly ICachedDatabaseProvider databaseProvider;
        private readonly IDbcStore dbcStore;
        private readonly ISpellStore spellStore;
        private readonly IMapAreaStore mapAreaStore;
        private readonly IWaypointProcessor waypointProcessor;
        private readonly IChatEmoteSoundProcessor chatProcessor;
        private readonly IRandomMovementDetector randomMovementDetector;
        private readonly IDbcSpellService spellService;
        private readonly IUpdateObjectFollower updateObjectFollower;
        private readonly IPlayerGuidFollower playerGuidFollower;
        private readonly ISpellCastProcessor spellCastProcessor;
        private readonly PrettyFlagParameter prettyFlagParameter;
        private readonly IFromGuidSpawnTimeProcessor fromGuidSpawnTimeProcessor;
        private readonly IAuraSlotTracker auraSlotTracker;
        private readonly HighLevelUpdateDump highLevelUpdateDump;
        private readonly IDespawnDetector despawnDetector;
        private WriterBuilder? writer = null;
        private Dictionary<UniversalGuid, WriterBuilder>? perGuidWriter = null;
        private readonly Dictionary<UniversalGuid, int> guids = new();
        private int currentShortGuid;
        private readonly Dictionary<uint, Dictionary<uint, string>> gossips = new();
        private HashSet<uint> activePhases = new();

        public bool RequiresSplitUpdateObject => true;

        private struct LastWorldState
        {
            public DateTime time;
            public int zoneId;
            public int areaId;
        }

        private LastWorldState? lastWorldState;

        public StoryTellerDumper(ICachedDatabaseProvider databaseProvider, 
            IDbcStore dbcStore,
            ISpellStore spellStore,
            IMapAreaStore mapAreaStore,
            IParameterFactory parameterFactory,
            IWaypointProcessor waypointProcessor,
            IChatEmoteSoundProcessor chatProcessor,
            IRandomMovementDetector randomMovementDetector,
            IDbcSpellService spellService,
            IUpdateObjectFollower updateObjectFollower,
            HighLevelUpdateDump highLevelUpdateDump,
            IDespawnDetector despawnDetector,
            IPlayerGuidFollower playerGuidFollower,
            ISpellCastProcessor spellCastProcessor,
            PrettyFlagParameter prettyFlagParameter,
            IFromGuidSpawnTimeProcessor fromGuidSpawnTimeProcessor,
            IAuraSlotTracker auraSlotTracker,
            bool perGuid) : base(waypointProcessor, chatProcessor, randomMovementDetector, despawnDetector, spellCastProcessor, auraSlotTracker)
        {
            this.databaseProvider = databaseProvider;
            this.dbcStore = dbcStore;
            this.spellStore = spellStore;
            this.mapAreaStore = mapAreaStore;
            this.waypointProcessor = waypointProcessor;
            this.chatProcessor = chatProcessor;
            this.randomMovementDetector = randomMovementDetector;
            this.spellService = spellService;
            this.updateObjectFollower = updateObjectFollower;
            this.playerGuidFollower = playerGuidFollower;
            this.spellCastProcessor = spellCastProcessor;
            this.prettyFlagParameter = prettyFlagParameter;
            this.fromGuidSpawnTimeProcessor = fromGuidSpawnTimeProcessor;
            this.auraSlotTracker = auraSlotTracker;
            this.highLevelUpdateDump = highLevelUpdateDump;
            this.despawnDetector = despawnDetector;

            if (perGuid)
                perGuidWriter = new();
            else
                writer = new WriterBuilder();
        }

        public override void Initialize(ulong gameBuild)
        {
            prettyFlagParameter.InitializeBuild(gameBuild);
            playerGuidFollower.Initialize(gameBuild);
            updateObjectFollower.Initialize(gameBuild);
            fromGuidSpawnTimeProcessor.Initialize(gameBuild);
            auraSlotTracker.Initialize(gameBuild);
            waypointProcessor.Initialize(gameBuild);
            chatProcessor.Initialize(gameBuild);
            spellCastProcessor.Initialize(gameBuild);
            despawnDetector.Initialize(gameBuild);
        }

        private string? pendingAppend;

        private void SetAppendOnNext(string? line)
        {
            pendingAppend = line;
        }

        private WriterBuilder GetWriterState(UniversalGuid? guid)
        {
            if (perGuidWriter == null)
                return writer!;
            if (guid == null)
                return WriterBuilder.Null;
            if (perGuidWriter.TryGetValue(guid.Value, out var wr))
                return wr;
            var writerBuilder = perGuidWriter[guid.Value] = new();
            return writerBuilder;
        }
        
        protected virtual void AppendLine(PacketBase packet, UniversalGuid? guid, string text, bool skipPacketNumber = false)
        {
            if (pendingAppend != null)
            {
                var pending = pendingAppend;
                pendingAppend = null;
                AppendLine(packet, guid, pending);
            }

            var state = GetWriterState(guid);
            
            if (!state.lastTime.HasValue)
                state.lastTime = packet.Time.ToDateTime();
            else
            {
                TimeSpan diff = packet.Time.ToDateTime().Subtract(state.lastTime.Value);
                if (diff.TotalMilliseconds > 130)
                {
                    state.writer.WriteLine();
                    if (perGuidWriter != null)
                        state.writer.Write("   ");
                    if (diff.TotalMilliseconds > 60000)
                        state.writer.WriteLine($"After {diff.ToNiceString()} ({diff.TotalMilliseconds} ms)");
                    else
                        state.writer.WriteLine($"After {diff.TotalMilliseconds} ms");
                    state.lastTime = packet.Time.ToDateTime();
                }
            }
            
            if (!skipPacketNumber)
                state.writer.WriteLine("     " + text + " ["+packet.Number+"]");
            else
                state.writer.WriteLine("     " + text);
        }
        
        public string? GetPrettyFormat(UniversalHighGuid type, uint id, int shortGuid = 0)
        {
            if (type == UniversalHighGuid.Creature || type == UniversalHighGuid.Vehicle || type == UniversalHighGuid.Pet)
            {
                var cr = databaseProvider.GetCachedCreatureTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id}) (GUID {shortGuid})";
            }
            else if (type == UniversalHighGuid.GameObject || type == UniversalHighGuid.Transport || type == UniversalHighGuid.WorldTransaction)
            {
                var cr = databaseProvider.GetCachedGameObjectTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id}) (GUID {shortGuid})";
            }
            return null;
        }
        
        private string NiceGuid(UniversalGuid? nullableGuid, bool withFull = false)
        {
            if (nullableGuid == null)
                return "(null guid)";
            var guid = nullableGuid.Value;
            int shortGuid = currentShortGuid;
            if (guids.ContainsKey(guid))
                shortGuid = guids[guid];
            else
                guids[guid] = currentShortGuid++;

            if (guid.Type == UniversalHighGuid.Player)
            {
                if (playerGuidFollower.PlayerGuid?.Equals(guid) ?? false)
                    return "Player (me)";
                return $"Player {shortGuid}";
            }
            if (guid.Type == UniversalHighGuid.Creature || guid.Type == UniversalHighGuid.Vehicle || guid.Type == UniversalHighGuid.Pet)
            {
                var pretty = GetPrettyFormat(guid.Type, guid.Entry, shortGuid);
                return pretty ?? "Creature " + guid.Entry + (shortGuid > 0 ? " (GUID " + shortGuid + ")" : "") + (withFull?" "+(guid.KindCase == UniversalGuid.KindOneofCase.Guid64 ? guid.Guid64.Low : guid.Guid128.Low).ToString("X8"):"");
            }
            if (guid.Type == UniversalHighGuid.GameObject || guid.Type == UniversalHighGuid.Transport)
            {
                var pretty = GetPrettyFormat(guid.Type, guid.Entry, shortGuid);
                return pretty ?? "GameObject " + guid.Entry + (shortGuid > 0 ? " (GUID " + shortGuid + ")" : "") + (withFull ? " " + (guid.KindCase == UniversalGuid.KindOneofCase.Guid64 ? guid.Guid64.Low : guid.Guid128.Low).ToString("X8") : "");
            }

            return guid.ToString()!;
        }

        private string GetStringFromDbc(Dictionary<long, string> dbc, int id)
        {
            if (dbc.TryGetValue(id, out var name))
                return $"{name} ({id})";
            return id.ToString();
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            var emote = chatProcessor.GetEmoteForChat(basePacket);
            var sound = chatProcessor.GetSoundForChat(basePacket);
            var text = chatProcessor.GetTextForChar(basePacket);
            AppendLine(basePacket,  packet.Sender, NiceGuid(packet.Sender) + " says: `" + text + "`"
                                   + (emote.HasValue ? " with emote " + GetStringFromDbc(dbcStore.EmoteStore, emote.Value) : "")
                                   + (sound.HasValue ? " with sound " + GetStringFromDbc(dbcStore.SoundStore, (int)sound.Value) : ""));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketEmote packet)
        {
            if (chatProcessor.IsEmoteForChat(basePacket))
                return false;
            AppendLine(basePacket, packet.Sender, NiceGuid(packet.Sender) + " plays emote: " + GetStringFromDbc(dbcStore.EmoteStore, packet.Emote));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySound packet)
        {
            if (chatProcessor.IsSoundForChat(basePacket))
                return false;
            AppendLine(basePacket, packet.Source, NiceGuid(packet.Source) + " plays sound: " + GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlayMusic packet)
        {
            AppendLine(basePacket, packet.Target, $"music: {packet.Music} plays");
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlayObjectSound packet)
        {
            if (chatProcessor.IsSoundForChat(basePacket))
                return false;
            AppendLine(basePacket, packet.Source, $"{NiceGuid(packet.Source)} plays object sound: {GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound)} to {NiceGuid(packet.Target)}");
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet)
        {
            if (packet.Updates.Count == 1)
            {
                var update = packet.Updates[0];
                if (update.Remove)
                {
                    var spellId = auraSlotTracker.GetSpellForAuraSlot(packet.Unit, update.Slot);
                    if (spellId.HasValue && spellService.Exists(spellId.Value))
                        AppendLine(basePacket, packet.Unit, NiceGuid(packet.Unit) + $" removed aura: {GetSpellName(spellId.Value)}");
                }
                else if (spellService.Exists(update.Spell))
                    AppendLine(basePacket, packet.Unit, NiceGuid(packet.Unit) + $" applied aura: {GetSpellName(update.Spell)}");
            }
            else
            {
                SetAppendOnNext(NiceGuid(packet.Unit) + " auras update:");
                foreach (ref readonly var update in packet.Updates.AsSpan())
                {
                    if (update.Remove)
                    {               
                        var spellId = auraSlotTracker.GetSpellForAuraSlot(packet.Unit, update.Slot);
                        if (spellId.HasValue && spellService.Exists(spellId.Value))
                            AppendLine(basePacket, packet.Unit, "    removed aura: " + GetSpellName(spellId.Value));
                    }
                    else if (!update.Remove)
                    {
                        if (spellService.Exists(update.Spell))
                            AppendLine(basePacket, packet.Unit, "    applied aura: " + GetSpellName(update.Spell));
                    }
                }
                SetAppendOnNext(null);   
            }
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellStart packet)
        {
            if (packet.Data == null)
                return false;

            if (!spellService.Exists(packet.Data->Spell))
                return false;

            UniversalGuid? castGuid = packet.Data->IdKindCase == PacketSpellData.IdKindOneofCase.CastGuid ? packet.Data->CastGuid : null;

            if (spellCastProcessor.HasFinishedCastingAt(castGuid, basePacket))
                return false;
            
            string verb = " starts casting: ";
            if (spellCastProcessor.HasFailedCastingAt(castGuid, basePacket))
                verb = " tries to cast and fails: ";

            AppendLine(basePacket, packet.Data->Caster, NiceGuid(packet.Data->Caster) + verb + GetSpellName(packet.Data->Spell));
            return true;
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
        {
            if (packet.Data == null)
                return false;

            if (!spellService.Exists(packet.Data->Spell))
                return false;
            
            string targetLine = "";
            int targetCount = packet.Data->HitTargets.Count;

            if (targetCount == 1)
            {
                targetLine = "at target: " + NiceGuid(packet.Data->HitTargets[0]);
            }
            else if (targetCount > 0)
            {
                targetLine = "at " + targetCount + " targets";

                int index = 0;
                targetLine += "\n       Spell Targets: {";
                foreach (ref readonly var guid in packet.Data->HitTargets.AsSpan())
                {
                    targetLine += "\n            [" + (index++) + "] " + NiceGuid(guid);
                }
                targetLine += "\n       }";
            }
            UniversalGuid? castGuid = packet.Data->IdKindCase == PacketSpellData.IdKindOneofCase.CastGuid ? packet.Data->CastGuid : null;

            string verb = "finishes casting";
            if (spellCastProcessor.HasStartedCastingAt(castGuid, basePacket))
                verb = "starts and finishes casting";
            
            AppendLine(basePacket, packet.Data->Caster, NiceGuid(packet.Data->Caster) + $" {verb}: " + GetSpellName(packet.Data->Spell) + " " + targetLine);
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellFailure packet)
        {
            if (!spellService.Exists(packet.Spell))
                return false;

            if (spellCastProcessor.HasStartedCastingAt(Unpack(packet.CastGuid), basePacket))
                return false;
            
            AppendLine(basePacket, packet.Caster, NiceGuid(packet.Caster) + $" failed casting spell " + GetSpellName(packet.Spell));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellCastFailed packet)
        {
            if (!spellService.Exists(packet.Spell))
                return false;

            if (spellCastProcessor.HasStartedCastingAt(Unpack(packet.CastGuid), basePacket))
                return false;

            AppendLine(basePacket, null, $"Casting spell " + GetSpellName(packet.Spell) + " failed");
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipMessage packet)
        {
            gossips[packet.MenuId] = new Dictionary<uint, string>();

            for (int i = 0; i < packet.Options.Count; ++i) 
                gossips[packet.MenuId].Add(packet.Options[i].OptionIndex, packet.Options[i].Text.ToString() ?? "");
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipSelect packet)
        {
            if (gossips.ContainsKey(packet.MenuId) && gossips[packet.MenuId].ContainsKey(packet.OptionId))
                AppendLine(basePacket, packet.GossipUnit, "Player choose option: " + gossips[packet.MenuId][packet.OptionId]);
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipHello packet)
        {
            AppendLine(basePacket, packet.GossipSource, "Player talks to: " + NiceGuid(packet.GossipSource));
            return base.Process(in basePacket, in packet);
        }

        private string GetSpellName(uint spellId)
        {
            return spellStore.HasSpell(spellId) ? spellStore.GetName(spellId) + " (" + spellId + ")" : $"spell {spellId}";
        }
        
        private string GetQuestName(uint questId)
        {
            var template = databaseProvider.GetCachedQuestTemplate(questId);
            return (template == null ? questId.ToString() : $"{template.Name} ({questId})");
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseGameObject packet)
        {
            AppendLine(basePacket, packet.GameObject, "Player uses gameobject: " + NiceGuid(packet.GameObject));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverAcceptQuest packet)
        {
            AppendLine(basePacket, packet.QuestGiver, "Player accepts quest: " + GetQuestName(packet.QuestId));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverQuestComplete packet)
        {
            AppendLine(basePacket, null, "Player rewards quest: " + GetQuestName(packet.QuestId));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestComplete packet)
        {
            AppendLine(basePacket, null, "Quest completed: " + GetQuestName(packet.QuestId));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestAddKillCredit packet)
        {
            AppendLine(basePacket, Unpack(packet.Victim), $"Added kill credit {packet.KillCredit} for quest " + GetQuestName(packet.QuestId) + $" ({packet.Count}/{packet.RequiredCount})");
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestFailed packet)
        {
            AppendLine(basePacket, null, "Quest failed: " + GetQuestName(packet.QuestId));
            return base.Process(in basePacket, in packet);
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellClick packet)
        {
            AppendLine(basePacket, packet.Target, "Player spell click on: " + NiceGuid(packet.Target));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            if (randomMovementDetector.RandomMovementPacketRatio(packet.Mover) > 0.65f)
                return false;

            bool lastPathSegmentHadOrientation = false;
            StringBuilder sb = new StringBuilder();

            if (packet.Flags.HasFlagFast(UniversalSplineFlag.TransportEnter))
                sb.Append("enters " +
                          (packet.TransportGuid.Type == UniversalHighGuid.Vehicle ? "vehicle" : "transport") + " " +
                          NiceGuid(packet.TransportGuid) + " on seat " + packet.VehicleSeat);
            else if (packet.Flags.HasFlagFast(UniversalSplineFlag.Parabolic) &&
                     packet.Points.Count == 1 && packet.PackedPoints.Count == 0 &&
                     packet.Jump != null)
            {
                var dest = packet.Points[0];
                sb.Append($"jumps to ({dest.X}, {dest.Y}, {dest.Z}) with gravity {packet.Jump->Gravity}");
            }
            else if (packet.Flags.HasFlagFast(UniversalSplineFlag.TransportExit))
                sb.Append("exits vehicle/transport");
            else if (packet.Points.Count > 0)
            {
                if (!waypointProcessor.State.TryGetValue(packet.Mover, out var state))
                    return true;

                var basePacketNumber = basePacket.Number;
                var path = state.Paths.FirstOrDefault(p => p.FirstPacketNumber == basePacketNumber);

                if (path == null)
                    return true;

                int i = 1;
                sb.AppendLine($"goes by waypoints [{TimeSpan.FromMilliseconds(path.TotalMoveTime).ToNiceString()} ({path.TotalMoveTime} ms)]: {{");
                foreach (var segment in path.Segments)
                {
                    if (segment.Wait.HasValue)
                        sb.AppendLine($"     wait {(uint)segment.Wait.Value.TotalMilliseconds} ms");
                    var length = segment.FinalLength();
                    sb.AppendLine($"     Segment {i++}, dist: {length}, average speed: {length / segment.MoveTime * 1000} yd/s");
                    if (segment.JumpGravity.HasValue)
                    {
                        var waypoint = segment.Waypoints[^1];
                        sb.AppendLine($"               jump to ({waypoint.X}, {waypoint.Y}, {waypoint.Z}) gravity: {segment.JumpGravity.Value} move time: {segment.MoveTime}");
                    }
                    else
                    {
                        for (var j = 0; j < segment.Waypoints.Count; j++)
                        {
                            var waypoint = segment.Waypoints[j];
                            if (segment.FinalOrientation.HasValue && j == segment.Waypoints.Count - 1)
                                sb.AppendLine($"               ({waypoint.X}, {waypoint.Y}, {waypoint.Z}, {segment.FinalOrientation.Value})");
                            else
                                sb.AppendLine($"               ({waypoint.X}, {waypoint.Y}, {waypoint.Z})");
                        }
                    }

                    lastPathSegmentHadOrientation = segment.FinalOrientation.HasValue;
                }
                sb.Append("       }");
                
                /*if (packet.Points.Count + packet.PackedPoints.Count == 1)
                {
                    var vec = packet.PackedPoints.Count == 1 ? packet.PackedPoints[0] : packet.Points[0];
                    sb.Append(" move time: " + packet.MoveTime);
                    sb.Append($" goes to: ({vec.X}, {vec.Y}, {vec.Z})");
                }
                else
                {
                    sb.Append(" move time: " + packet.MoveTime);
                    if ((packet.Flags & UniversalSplineFlag.UncompressedPath) != 0)
                    {
                        Debug.Assert(packet.PackedPoints.Count == 0);
                        sb.AppendLine(" goes by " + (packet.Points.Count) + " waypoints: {");

                        foreach (var point in packet.Points)
                            sb.AppendLine($"               ({point.X}, {point.Y}, {point.Z})");
                        sb.Append("       }");
                    }
                    else
                    {
                        Debug.Assert(packet.Points.Count == 1);
                        sb.AppendLine(" goes by " + (packet.PackedPoints.Count + 1) + " waypoints: {");

                        foreach (var waypoint in packet.PackedPoints)
                            sb.AppendLine($"               ({waypoint.X}, {waypoint.Y}, {waypoint.Z})");
                        sb.Append("       }");   

                        sb.AppendLine($" with destination ({packet.Points[0].X}, {packet.Points[0].Y}, {packet.Points[0].Z})");
                    }
                }*/
                
                
                if (packet.Jump != null && (packet.Flags & UniversalSplineFlag.Parabolic) != 0)
                {
                    if (sb.Length > 0)
                        sb.Append("\n    then ");
                
                    sb.Append($"after [special time] jump with gravity {packet.Jump->Gravity}");
                    sb.Append("\n    (explanation: this packet is both a path and a jump. Check the packet text view manually, because story teller doesn't support it fully)");
                }
            }

            var skipOrientation = lastPathSegmentHadOrientation &&
                                  packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookOrientation;
            if (packet.FacingCase != PacketMonsterMove.FacingOneofCase.None && !skipOrientation)
            {
                if (sb.Length > 0)
                    sb.Append("\n    then ");
                if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookOrientation)
                    sb.Append("set orientation to " + packet.LookOrientation);
                else if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookTarget)
                    sb.Append("looks at " + NiceGuid(packet.LookTarget.Target));
                else if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookPosition)
                    sb.Append($"looks at ({packet.LookPosition.X}, {packet.LookPosition.Y}, {packet.LookPosition.Z})");   
            }
            
            if (sb.Length == 0)
                sb.Append("stops");

            var randomRatio = randomMovementDetector.RandomMovementPacketRatio(packet.Mover);
            AppendLine(basePacket, packet.Mover, NiceGuid(packet.Mover) + $" [random movement chance: {randomRatio*100:0}%] " + sb);
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketInitWorldStates packet)
        {
            lastWorldState = new LastWorldState()
            {
                time = basePacket.Time.ToDateTime(),
                zoneId = packet.ZoneId,
                areaId = packet.AreaId
            };
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPhaseShift packet)
        {
            StringBuilder sb = new StringBuilder();
            List<string> phases = new List<string>();
            List<string> addedPhases = new List<string>();
            List<string> removedPhases = new List<string>();
            List<string> continuedPhases = new List<string>();

            foreach (uint phase in packet.Phases.AsSpan())
            {
                phases.Add(GetStringFromDbc(dbcStore.PhaseStore, (int)phase));
                bool wasadded = activePhases.Add(phase);
                if (wasadded)
                {
                    addedPhases.Add(GetStringFromDbc(dbcStore.PhaseStore, (int)phase));
                }
            }

            foreach (uint phase in activePhases.ToList())
            {
                bool isRemoved = !packet.Phases.AsSpan().Contains(phase);
                if (isRemoved)
                {
                    activePhases.Remove(phase);
                    removedPhases.Add(GetStringFromDbc(dbcStore.PhaseStore, (int)phase));
                }
            }

            foreach (uint phase in activePhases.ToList())
            {
                bool iscontinued = !addedPhases.Contains(GetStringFromDbc(dbcStore.PhaseStore, (int)phase));
                if (iscontinued)
                { 
                    continuedPhases.Add(GetStringFromDbc(dbcStore.PhaseStore, (int)phase));
                }
            }

            if (continuedPhases.Count > 0)
                sb.Append("Continued Phases:    " + continuedPhases[0]);
            if ((continuedPhases.Count > 1 || addedPhases.Count > 0 || removedPhases.Count > 0) && continuedPhases.Count > 0)
                sb.AppendLine();
            for (int i = 1; i < continuedPhases.Count; ++i)
            {
                sb.Append("                          " + continuedPhases[i]);
                if (i < continuedPhases.Count - 1 || addedPhases.Count > 0 || removedPhases.Count > 0)
                    sb.AppendLine();
            }
            if (addedPhases.Count > 0 && continuedPhases.Count > 0)
                sb.Append("     Added Phases:        " + addedPhases[0]);
            if (addedPhases.Count > 0 && continuedPhases.Count == 0)
                sb.Append("Added Phases:        " + addedPhases[0]);
            if ((addedPhases.Count > 1 || removedPhases.Count > 0) && addedPhases.Count > 0)
                sb.AppendLine();
            for (int i = 1; i < addedPhases.Count; ++i)
            {
                sb.Append("                          " + addedPhases[i]);
                if (i < addedPhases.Count - 1 || removedPhases.Count > 0)
                    sb.AppendLine();
            }

            if (removedPhases.Count > 0)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                    sb.Append("     ");
                }
                sb.Append("Removed Phases:      " + removedPhases[0]);
            }

            if (removedPhases.Count > 1)
                sb.AppendLine();
            for (int i = 1; i < removedPhases.Count; ++i)
            {
                sb.Append("                          " + removedPhases[i]);
                if (i < removedPhases.Count - 1)
                    sb.AppendLine();
            }

            if (lastWorldState.HasValue)
            {
                var zoneName = mapAreaStore.GetAreaById((uint)lastWorldState.Value.zoneId)?.Name;
                var areaName = mapAreaStore.GetAreaById((uint)lastWorldState.Value.areaId)?.Name;
                var diff = basePacket.Time.ToDateTime() - lastWorldState.Value.time;
                sb.Append($"           Last zone: {zoneName} ({lastWorldState.Value.zoneId}) Last area: {areaName} ({lastWorldState.Value.areaId}) ({diff.ToHumanFriendlyString()} ago)");
            }

            AppendLine(basePacket, null, sb.ToString());
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseItem packet)
        {
            AppendLine(basePacket, null, "Player uses item in backpack and cast spell " + GetSpellName(packet.SpellId));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketOneShotAnimKit packet)
        {
            AppendLine(basePacket, packet.Unit, NiceGuid(packet.Unit) + " plays one shot anim kit " + packet.AnimKit);
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGameObjectCustomAnim packet)
        {
            AppendLine(basePacket, packet.GameObject, NiceGuid(packet.GameObject) + " plays custom anim " + packet.Anim);
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientAreaTrigger packet)
        {
            AppendLine(basePacket, null, "Player " + (packet.Enter ? "enters" : "leaves") + " clientside area trigger " + packet.AreaTrigger);
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSetAnimKit packet)
        {
            AppendLine(basePacket, packet.Unit, NiceGuid(packet.Unit) + " sets anim kit " + packet.AnimKit);
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            foreach (ref readonly var destroyed in packet.Destroyed.AsSpan())
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                AppendLine(basePacket, destroyed.Guid, "Destroyed " + NiceGuid(destroyed.Guid));
            }

            foreach (ref readonly var destroyed in packet.OutOfRange.AsSpan())
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                AppendLine(basePacket, destroyed.Guid, "Out of range: " + NiceGuid(destroyed.Guid));
            }

            foreach (ref readonly var created in packet.Created.AsSpan())
            {
                if (created.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                var spawnTime = despawnDetector.GetSpawnLength(created.Guid, basePacket.Number);
                var createType = created.CreateType == CreateObjectType.InRange ? "In range " : "Spawned ";
                var spawnedAgo = fromGuidSpawnTimeProcessor.TryGetSpawnTime(created.Guid, basePacket.Time.ToDateTime());
                SetAppendOnNext(createType + NiceGuid(created.Guid) + " at " + VecToString(created.Movement != null ? created.Movement->Position : created.Stationary != null ? created.Stationary->Position : null,
                                    created.Movement != null ? created.Movement->Orientation : created.Stationary != null ? created.Stationary->Orientation : null) +
                                (spawnTime == null ? "" : $" (will be destroyed in {spawnTime.Value.ToNiceString()})") +
                                (spawnedAgo.HasValue && (spawnedAgo.Value.TotalMilliseconds > 1000 || created.CreateType == CreateObjectType.InRange) ? $" (spawned {spawnedAgo.Value.ToNiceString()} ago)" : ""));
                PrintValues(basePacket, created.Guid, created.Values, false);
                SetAppendOnNext(null);
            }
            
            foreach (ref readonly var updated in packet.Updated.AsSpan())
            {
                if (updated.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                if (updated.Values.TryGetInt("UNIT_FIELD_HEALTH", out var hp) &&
                    hp == 0)
                {
                    var oldHp = updateObjectFollower.GetInt(updated.Guid, "UNIT_FIELD_HEALTH") ?? 1;
                    if (oldHp != 0)
                        AppendLine(basePacket, updated.Guid, NiceGuid(updated.Guid) + " dies");
                }
                
                if (updated.Values.TryGetInt("UNIT_FIELD_FLAGS", out var unitFlags))
                {
                    long old = 0;
                    updateObjectFollower.TryGetIntOrDefault(updated.Guid, "UNIT_FIELD_FLAGS", out old);

                    var inCombat = (unitFlags & (long)GameDefines.UnitFlags.InCombat) > 0;
                    var wasInCombat = (old & (long)GameDefines.UnitFlags.InCombat) > 0;
                    
                    if (wasInCombat && !inCombat)
                        AppendLine(basePacket, updated.Guid, NiceGuid(updated.Guid) + " exits combat");
                    else if (!wasInCombat && inCombat)
                        AppendLine(basePacket, updated.Guid, NiceGuid(updated.Guid) + " enters combat");
                }
                
                SetAppendOnNext("Updated " + NiceGuid(updated.Guid));
                PrintValues(basePacket, updated.Guid, updated.Values, true);
                SetAppendOnNext(null);
            }
            return base.Process(in basePacket, in packet);
        }

        private string VecToString(Vec3? pos, float? orientation)
        {
            if (pos == null)
                return "(unknown)";
            if (orientation.HasValue)
                return $"({pos.Value.X}, {pos.Value.Y}, {pos.Value.Z}, {orientation})";
            else
                return $"({pos.Value.X}, {pos.Value.Y}, {pos.Value.Z})";
        }

        private string? TryGenerateFlagsDiff(IParameter<long>? param, string key, long old, long nnew)
        {
            if (param == null || !param.HasItems)
                return null;

            if (param is not FlagParameter fp)
                return null;

            List<string> flags = new();
            for (int i = 0; i < 32; ++i)
            {
                long flag = (1L << i);
                var oldHad = (old & flag) == flag;
                var newHas = (nnew & flag) == flag;
                if (!fp.Items!.TryGetValue(flag, out var flagName))
                    continue;
                
                if (oldHad && !newHas)
                    flags.Add($" (-) {flagName.Name}");
                if (oldHad && newHas)
                    flags.Add($" {flagName.Name}");
                if (!oldHad && newHas)
                    flags.Add($" (+) {flagName.Name}");
            }

            return string.Join(", ", flags);
        }

        private void PrintValues(PacketBase basePacket, UniversalGuid guid, UpdateValues values, bool isUpdate)
        {
            foreach (var val in values.Ints())
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                    continue;
                var newValue = RemoveUselessFlag(val.Key, val.Value, guid);

                if (!isUpdate || !updateObjectFollower.TryGetIntOrDefault(guid, val.Key, out var intValue))
                {
                    var param = prettyFlagParameter.GetPrettyParameter(val.Key);
                    var stringValue = param == null ? "" : $" [{param.ToString(newValue)}]";
                    AppendLine(basePacket, guid, $"     {val.Key} = {newValue}{stringValue}", true);
                }
                else if (RemoveUselessFlag(val.Key, intValue, guid) != newValue)
                {
                    intValue = RemoveUselessFlag(val.Key, intValue, guid);
                    var param = prettyFlagParameter.GetPrettyParameter(val.Key);
                    var oldStringValue = param == null ? "" : $" [{param.ToString(intValue)}]";
                    var newStringValue = param == null ? "" : $" [{param.ToString(newValue)}]";
                    var change = TryGenerateFlagsDiff(param, val.Key, intValue, newValue);
                    if (change == null)
                        AppendLine(basePacket, guid, $"     {val.Key} = {newValue}{newStringValue} (old: {intValue}{oldStringValue})", true);
                    else
                        AppendLine(basePacket, guid, $"     {val.Key} = {newValue} (old: {intValue}) {change}", true);

                    foreach (var extra in highLevelUpdateDump.Produce(val.Key, (uint)intValue, (uint)newValue))
                        AppendLine(basePacket, guid, $"       -> {extra}", true);   
                    
                }
            }
            
            foreach (var val in values.Floats())
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                    continue;
                
                if (!isUpdate || !updateObjectFollower.TryGetFloat(guid, val.Key, out var intValue))
                    AppendLine(basePacket, guid, $"     {val.Key} = {val.Value}", true);
                else if (Math.Abs(intValue - val.Value) > 0.01f)
                    AppendLine(basePacket, guid, $"     {val.Key} = {val.Value} (old: {intValue})", true);
            }
            
            foreach (var val in values.Guids())
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                    continue;
                
                if (!isUpdate || !updateObjectFollower.TryGetGuid(guid, val.Key, out var intValue))
                    AppendLine(basePacket, guid, $"     {val.Key} = {val.Value}", true);
                else if (!intValue.Equals(val.Value))
                    AppendLine(basePacket, guid, $"     {val.Key} = {val.Value} (old: {intValue})", true);
            }
        }
        
        private long RemoveUselessFlag(string field, long value, UniversalGuid guid)
        {
            if (field == "UNIT_FIELD_FLAGS")
            {
                value = value & ~ (uint)GameDefines.UnitFlags.ServerSideControlled;
                if (guid.Type == UniversalHighGuid.Player)
                    value = value & ~(uint)GameDefines.UnitFlags.Looting;
            }

            return value;
        }

        private bool IsUpdateFieldInteresting(string field, bool isUpdate)
        {
            switch (field)
            {
                case "UNIT_FIELD_BYTES_0":
                case "UNIT_FIELD_BYTES_1":
                case "UNIT_FIELD_BYTES_2":
                case "UNIT_FIELD_FACTIONTEMPLATE":
                case "UNIT_FIELD_FLAGS":
                case "UNIT_FIELD_FLAGS_2":
                case "UNIT_NPC_EMOTESTATE":
                case "UNIT_NPC_FLAGS":
                case "GAMEOBJECT_BYTES_1":
                    return true;
            }

            if (isUpdate)
            {
                switch (field)
                {
                    case "UNIT_FIELD_DISPLAYID":
                    case "OBJECT_FIELD_SCALE_X":
                    case "UNIT_FIELD_LEVEL":
                    case "UNIT_VIRTUAL_ITEM_SLOT_ID":
                    case "GAMEOBJECT_FACTION":
                        return true;
                }
            }

            return false;
        }

        public async Task<string> Generate()
        {
            StringBuilder totalBuilder = new();
            if (perGuidWriter == null)
                return writer!.builder.ToString();
            foreach (var guid in perGuidWriter)
            {
                var universalGuid = guid.Key;
                totalBuilder.AppendLine(universalGuid.ToWowParserString());
                totalBuilder.AppendLine(NiceGuid(guid.Key));
                totalBuilder.AppendLine(guid.Value.builder.ToString());
                totalBuilder.AppendLine("\n\n\n\n");
            }
            return totalBuilder.ToString();
        }

        public override bool Process(ref readonly PacketHolder packet)
        {
            playerGuidFollower.Process(in packet);
            var ret = base.Process(in packet);
            updateObjectFollower.Process(in packet);
            return ret;
        }

        public void ProcessUnfiltered(ref PacketHolder unfiltered)
        {
            if (unfiltered.KindCase == PacketHolder.KindOneofCase.UpdateObject)
                updateObjectFollower.Process(ref unfiltered);
        }

        public bool UnfilteredPreProcess(ref readonly PacketHolder packet)
        {
            return fromGuidSpawnTimeProcessor.Process(in packet);
        }
    }
}
