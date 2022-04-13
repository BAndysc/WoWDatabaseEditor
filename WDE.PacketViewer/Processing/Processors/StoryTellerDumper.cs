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

namespace WDE.PacketViewer.Processing.Processors
{
    [AutoRegister]
    public class StoryTellerDumper : CompoundProcessor<bool, IWaypointProcessor, IChatEmoteSoundProcessor, IRandomMovementDetector, IDespawnDetector, ISpellCastProcessor, IFromGuidSpawnTimeProcessor>,
        IPacketTextDumper, ITwoStepPacketBoolProcessor, IUnfilteredPacketProcessor
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
        
        private readonly IDatabaseProvider databaseProvider;
        private readonly IDbcStore dbcStore;
        private readonly ISpellStore spellStore;
        private readonly IWaypointProcessor waypointProcessor;
        private readonly IChatEmoteSoundProcessor chatProcessor;
        private readonly IRandomMovementDetector randomMovementDetector;
        private readonly ISpellService spellService;
        private readonly IUpdateObjectFollower updateObjectFollower;
        private readonly IPlayerGuidFollower playerGuidFollower;
        private readonly ISpellCastProcessor spellCastProcessor;
        private readonly PrettyFlagParameter prettyFlagParameter;
        private readonly IFromGuidSpawnTimeProcessor fromGuidSpawnTimeProcessor;
        private readonly HighLevelUpdateDump highLevelUpdateDump;
        private readonly IDespawnDetector despawnDetector;
        private WriterBuilder? writer = null;
        private Dictionary<UniversalGuid, WriterBuilder>? perGuidWriter = null;
        private readonly Dictionary<UniversalGuid, int> guids = new();
        private int currentShortGuid;
        private readonly Dictionary<UniversalGuid, Dictionary<int, uint>> auras = new();
        private readonly Dictionary<uint, Dictionary<uint, string>> gossips = new();

        public bool RequiresSplitUpdateObject => true;
        
        public StoryTellerDumper(IDatabaseProvider databaseProvider, 
            IDbcStore dbcStore,
            ISpellStore spellStore,
            IParameterFactory parameterFactory,
            IWaypointProcessor waypointProcessor,
            IChatEmoteSoundProcessor chatProcessor,
            IRandomMovementDetector randomMovementDetector,
            ISpellService spellService,
            IUpdateObjectFollower updateObjectFollower,
            HighLevelUpdateDump highLevelUpdateDump,
            IDespawnDetector despawnDetector,
            IPlayerGuidFollower playerGuidFollower,
            ISpellCastProcessor spellCastProcessor,
            PrettyFlagParameter prettyFlagParameter,
            IFromGuidSpawnTimeProcessor fromGuidSpawnTimeProcessor,
            bool perGuid) : base(waypointProcessor, chatProcessor, randomMovementDetector, despawnDetector, spellCastProcessor, fromGuidSpawnTimeProcessor)
        {
            this.databaseProvider = databaseProvider;
            this.dbcStore = dbcStore;
            this.spellStore = spellStore;
            this.waypointProcessor = waypointProcessor;
            this.chatProcessor = chatProcessor;
            this.randomMovementDetector = randomMovementDetector;
            this.spellService = spellService;
            this.updateObjectFollower = updateObjectFollower;
            this.playerGuidFollower = playerGuidFollower;
            this.spellCastProcessor = spellCastProcessor;
            this.prettyFlagParameter = prettyFlagParameter;
            this.fromGuidSpawnTimeProcessor = fromGuidSpawnTimeProcessor;
            this.highLevelUpdateDump = highLevelUpdateDump;
            this.despawnDetector = despawnDetector;

            if (perGuid)
                perGuidWriter = new();
            else
                writer = new WriterBuilder();
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
            if (perGuidWriter.TryGetValue(guid, out var wr))
                return wr;
            var writerBuilder = perGuidWriter[guid] = new();
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
                var cr = databaseProvider.GetCreatureTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id}) (GUID {shortGuid})";
            }
            else if (type == UniversalHighGuid.GameObject || type == UniversalHighGuid.Transport || type == UniversalHighGuid.WorldTransaction)
            {
                var cr = databaseProvider.GetGameObjectTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id}) (GUID {shortGuid})";
            }
            return null;
        }
        
        private string NiceGuid(UniversalGuid? guid, bool withFull = false)
        {
            if (guid == null)
                return "(null guid)";
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
                return pretty ?? "Creature " + guid.Entry + (shortGuid > 0 ? " (GUID " + shortGuid + ")" : "") + (withFull?" "+(guid.Guid64?.Low ?? guid.Guid128.Low).ToString("X8"):"");
            }
            if (guid.Type == UniversalHighGuid.GameObject || guid.Type == UniversalHighGuid.Transport)
            {
                var pretty = GetPrettyFormat(guid.Type, guid.Entry, shortGuid);
                return pretty ?? "GameObject " + guid.Entry + (shortGuid > 0 ? " (GUID " + shortGuid + ")" : "") + (withFull ? " " + (guid.Guid64?.Low ?? guid.Guid128.Low).ToString("X8") : "");
            }

            return guid.ToString();
        }

        private string GetStringFromDbc(Dictionary<long, string> dbc, int id)
        {
            if (dbc.TryGetValue(id, out var name))
                return $"{name} ({id})";
            return id.ToString();
        }
        
        protected override bool Process(PacketBase basePacket, PacketChat packet)
        {
            var emote = chatProcessor.GetEmoteForChat(basePacket);
            var sound = chatProcessor.GetSoundForChat(basePacket);
            AppendLine(basePacket,  packet.Sender, NiceGuid(packet.Sender) + " says: `" + packet.Text + "`"
                                   + (emote.HasValue ? " with emote " + GetStringFromDbc(dbcStore.EmoteStore, emote.Value) : "")
                                   + (sound.HasValue ? " with sound " + GetStringFromDbc(dbcStore.SoundStore, (int)sound.Value) : ""));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketEmote packet)
        {
            if (chatProcessor.IsEmoteForChat(basePacket))
                return false;
            AppendLine(basePacket, packet.Sender, NiceGuid(packet.Sender) + " plays emote: " + GetStringFromDbc(dbcStore.EmoteStore, packet.Emote));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlaySound packet)
        {
            if (chatProcessor.IsSoundForChat(basePacket))
                return false;
            AppendLine(basePacket, packet.Source, NiceGuid(packet.Source) + " plays sound: " + GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayMusic packet)
        {
            AppendLine(basePacket, packet.Target, $"music: {packet.Music} plays");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayObjectSound packet)
        {
            if (chatProcessor.IsSoundForChat(basePacket))
                return false;
            AppendLine(basePacket, packet.Source, $"{NiceGuid(packet.Source)} plays object sound: {GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound)} to {NiceGuid(packet.Target)}");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            if (!auras.ContainsKey(packet.Unit))
                auras[packet.Unit] = new Dictionary<int, uint>();
            SetAppendOnNext(NiceGuid(packet.Unit) + " auras update:");
            foreach (var update in packet.Updates)
            {
                if (update.Remove && auras[packet.Unit].ContainsKey(update.Slot))
                {
                    if (spellService.Exists(auras[packet.Unit][update.Slot]))
                        AppendLine(basePacket, packet.Unit,
                        "    removed aura: " + GetSpellName(auras[packet.Unit][update.Slot]));
                    auras[packet.Unit].Remove(update.Slot);
                }
                else if (!update.Remove)
                {
                    auras[packet.Unit][update.Slot] = update.Spell;
                    
                    if (spellService.Exists(update.Spell))
                        AppendLine(basePacket, packet.Unit, "    applied aura: " + GetSpellName(update.Spell));
                }
            }
            SetAppendOnNext(null);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellStart packet)
        {
            if (!spellService.Exists(packet.Data.Spell))
                return false;

            if (spellCastProcessor.HasFinishedCastingAt(packet.Data.CastGuid, basePacket))
                return false;
            
            string verb = " starts casting ";
            if (spellCastProcessor.HasFailedCastingAt(packet.Data.CastGuid, basePacket))
                verb = " tries to cast and fails ";

            AppendLine(basePacket, packet.Data.Caster, NiceGuid(packet.Data.Caster) + verb + GetSpellName(packet.Data.Spell));
            return true;
        }
        
        protected override bool Process(PacketBase basePacket, PacketSpellGo packet)
        {
            if (!spellService.Exists(packet.Data.Spell))
                return false;
            
            string targetLine = "";
            int targetCount = packet.Data.HitTargets.Count;

            if (targetCount == 1)
            {
                targetLine = "at target: " + NiceGuid(packet.Data.HitTargets[0]);
            }
            else if (targetCount > 0)
            {
                targetLine = "at " + targetCount + " targets";

                int index = 0;
                targetLine += "\n       Spell Targets: {";
                foreach (var guid in packet.Data.HitTargets)
                {
                    targetLine += "\n            [" + (index++) + "] " + NiceGuid(guid);
                }
                targetLine += "\n       }";
            }

            string verb = "finishes casting";
            if (spellCastProcessor.HasStartedCastingAt(packet.Data.CastGuid, basePacket))
                verb = "starts and finishes casting";
            
            AppendLine(basePacket, packet.Data.Caster, NiceGuid(packet.Data.Caster) + $" {verb}: " + GetSpellName(packet.Data.Spell) + " " + targetLine);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellFailure packet)
        {
            if (!spellService.Exists(packet.Spell))
                return false;

            if (spellCastProcessor.HasStartedCastingAt(packet.CastGuid, basePacket))
                return false;
            
            AppendLine(basePacket, packet.Caster, NiceGuid(packet.Caster) + $" failed casting spell " + GetSpellName(packet.Spell));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellCastFailed packet)
        {
            if (!spellService.Exists(packet.Spell))
                return false;

            if (spellCastProcessor.HasStartedCastingAt(packet.CastGuid, basePacket))
                return false;

            AppendLine(basePacket, null, $"Casting spell " + GetSpellName(packet.Spell) + " failed");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipMessage packet)
        {
            gossips[packet.MenuId] = new Dictionary<uint, string>();

            for (int i = 0; i < packet.Options.Count; ++i) 
                gossips[packet.MenuId].Add(packet.Options[i].OptionIndex, packet.Options[i].Text);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipSelect packet)
        {
            if (gossips.ContainsKey(packet.MenuId) && gossips[packet.MenuId].ContainsKey(packet.OptionId))
                AppendLine(basePacket, packet.GossipUnit, "Player choose option: " + gossips[packet.MenuId][packet.OptionId]);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipHello packet)
        {
            AppendLine(basePacket, packet.GossipSource, "Player talks to: " + NiceGuid(packet.GossipSource));
            return base.Process(basePacket, packet);
        }

        private string GetSpellName(uint spellId)
        {
            return spellStore.HasSpell(spellId) ? spellStore.GetName(spellId) + " (" + spellId + ")" : $"spell {spellId}";
        }
        
        private string GetQuestName(uint questId)
        {
            var template = databaseProvider.GetQuestTemplate(questId);
            return (template == null ? questId.ToString() : $"{template.Name} ({questId})");
        }

        protected override bool Process(PacketBase basePacket, PacketClientUseGameObject packet)
        {
            AppendLine(basePacket, packet.GameObject, "Player uses gameobject: " + NiceGuid(packet.GameObject));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestGiverAcceptQuest packet)
        {
            AppendLine(basePacket, packet.QuestGiver, "Player accepts quest: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestGiverQuestComplete packet)
        {
            AppendLine(basePacket, null, "Player rewards quest: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestComplete packet)
        {
            AppendLine(basePacket, null, "Quest completed: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestAddKillCredit packet)
        {
            AppendLine(basePacket, packet.Victim, $"Added kill credit {packet.KillCredit} for quest " + GetQuestName(packet.QuestId) + $" ({packet.Count}/{packet.RequiredCount})");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestFailed packet)
        {
            AppendLine(basePacket, null, "Quest failed: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }
        
        protected override bool Process(PacketBase basePacket, PacketSpellClick packet)
        {
            AppendLine(basePacket, packet.Target, "Player spell click on: " + NiceGuid(packet.Target));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            if (randomMovementDetector.RandomMovementPacketRatio(packet.Mover) > 0.65f)
                return false;

            bool lastPathSegmentHadOrientation = false;
            StringBuilder sb = new StringBuilder();

            if (packet.Flags.HasFlag(UniversalSplineFlag.TransportEnter))
                sb.Append("enters " +
                          (packet.TransportGuid.Type == UniversalHighGuid.Vehicle ? "vehicle" : "transport") + " " +
                          NiceGuid(packet.TransportGuid) + " on seat " + packet.VehicleSeat);
            else if (packet.Flags.HasFlag(UniversalSplineFlag.Parabolic) &&
                     packet.Points.Count == 1 && packet.PackedPoints.Count == 0 &&
                     packet.Jump != null)
            {
                var dest = packet.Points[0];
                sb.Append($"jumps to ({dest.X}, {dest.Y}, {dest.Z}) with gravity {packet.Jump.Gravity}");
            }
            else if (packet.Flags.HasFlag(UniversalSplineFlag.TransportExit))
                sb.Append("exits vehicle/transport");
            else if (packet.Points.Count > 0)
            {
                if (!waypointProcessor.State.TryGetValue(packet.Mover, out var state))
                    return true;

                var path = state.Paths.FirstOrDefault(p => p.FirstPacketNumber == basePacket.Number);

                if (path == null)
                    return true;

                int i = 1;
                sb.AppendLine($"goes by waypoints [{TimeSpan.FromMilliseconds(path.TotalMoveTime).ToNiceString()} ({path.TotalMoveTime} ms)]: {{");
                foreach (var segment in path.Segments)
                {
                    sb.AppendLine($"     Segment {i++}, dist: {segment.OriginalDistance}, average speed: {segment.OriginalDistance / segment.MoveTime * 1000} yd/s");
                    for (var j = 0; j < segment.Waypoints.Count; j++)
                    {
                        var waypoint = segment.Waypoints[j];
                        if (segment.FinalOrientation.HasValue && j == segment.Waypoints.Count - 1)
                            sb.AppendLine($"               ({waypoint.X}, {waypoint.Y}, {waypoint.Z}, {segment.FinalOrientation.Value})");
                        else
                            sb.AppendLine($"               ({waypoint.X}, {waypoint.Y}, {waypoint.Z})");
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
                
                    sb.Append($"after [special time] jump with gravity {packet.Jump.Gravity}");
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
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPhaseShift packet)
        {
            StringBuilder sb = new StringBuilder();
            List<string> phases = new List<string>();
            foreach (uint phase in packet.Phases)
            {
                phases.Add(GetStringFromDbc(dbcStore.PhaseStore, (int)phase));
            }

            if (phases.Count > 0)
                sb.AppendLine("switch phase to: "+ phases[0]);
            for (int i = 1; i < phases.Count; ++i)
            {
                sb.Append("                    " + phases[i]);
                if (i < phases.Count - 1)
                    sb.AppendLine();
            }

            AppendLine(basePacket, null, sb.ToString());
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketClientUseItem packet)
        {
            AppendLine(basePacket, null, "Player uses item in backpack and cast spell " + GetSpellName(packet.SpellId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketOneShotAnimKit packet)
        {
            AppendLine(basePacket, packet.Unit, NiceGuid(packet.Unit) + " plays one shot anim kit " + packet.AnimKit);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGameObjectCustomAnim packet)
        {
            AppendLine(basePacket, packet.GameObject, NiceGuid(packet.GameObject) + " plays custom anim " + packet.Anim);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketClientAreaTrigger packet)
        {
            AppendLine(basePacket, null, "Player " + (packet.Enter ? "enters" : "leaves") + " clientside area trigger " + packet.AreaTrigger);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSetAnimKit packet)
        {
            AppendLine(basePacket, packet.Unit, NiceGuid(packet.Unit) + " sets anim kit " + packet.AnimKit);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var destroyed in packet.Destroyed)
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                AppendLine(basePacket, destroyed.Guid, "Destroyed " + NiceGuid(destroyed.Guid));
            }

            foreach (var destroyed in packet.OutOfRange)
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                AppendLine(basePacket, destroyed.Guid, "Out of range: " + NiceGuid(destroyed.Guid));
            }

            foreach (var created in packet.Created)
            {
                if (created.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                var spawnTime = despawnDetector.GetSpawnLength(created.Guid, basePacket.Number);
                var createType = created.CreateType == CreateObjectType.InRange ? "In range " : "Spawned ";
                var spawnedAgo = fromGuidSpawnTimeProcessor.TryGetSpawnTime(created.Guid, basePacket.Time.ToDateTime());
                SetAppendOnNext(createType + NiceGuid(created.Guid) + " at " + VecToString(created.Movement?.Position ?? created.Stationary?.Position, created.Movement?.Orientation ?? created.Stationary?.Orientation) +
                                (spawnTime == null ? "" : $" (will be destroyed in {spawnTime.Value.ToNiceString()})") +
                                (spawnedAgo.HasValue && spawnedAgo.Value.TotalMilliseconds > 1000 ? $" (spawned {spawnedAgo.Value.ToNiceString()} ago)" : ""));
                PrintValues(basePacket, created.Guid, created.Values, false);
                SetAppendOnNext(null);
            }
            
            foreach (var updated in packet.Updated)
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
            return base.Process(basePacket, packet);
        }

        private string VecToString(Vec3? pos, float? orientation)
        {
            if (pos == null)
                return "(unknown)";
            if (orientation.HasValue)
                return $"({pos.X}, {pos.Y}, {pos.Z}, {orientation})";
            else
                return $"({pos.X}, {pos.Y}, {pos.Z})";
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
                totalBuilder.AppendLine(guid.Key.ToWowParserString());
                totalBuilder.AppendLine(NiceGuid(guid.Key));
                totalBuilder.AppendLine(guid.Value.builder.ToString());
                totalBuilder.AppendLine("\n\n\n\n");
            }
            return totalBuilder.ToString();
        }

        public override bool Process(PacketHolder packet)
        {
            playerGuidFollower.Process(packet);
            var ret = base.Process(packet);
            updateObjectFollower.Process(packet);
            return ret;
        }

        public void ProcessUnfiltered(PacketHolder unfiltered)
        {
            if (unfiltered.KindCase == PacketHolder.KindOneofCase.UpdateObject)
                updateObjectFollower.Process(unfiltered);
        }
    }
}