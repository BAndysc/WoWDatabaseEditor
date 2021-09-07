using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;
using WDE.PacketViewer.Processing.Runners;
using WDE.PacketViewer.Utils;

namespace WDE.PacketViewer.Processing.Processors
{
    [AutoRegister]
    public class StoryTellerDumper : CompoundProcessor<bool, IWaypointProcessor, IChatEmoteSoundProcessor, IRandomMovementDetector>,
        IPacketTextDumper, ITwoStepPacketBoolProcessor
    {
        private class Entity
        {
            public Dictionary<string, long> Ints { get; } = new();
            public Dictionary<string, float> Floats { get; } = new();
            public Dictionary<string, UniversalGuid> Guids { get; } = new();
        }

        private readonly IDatabaseProvider databaseProvider;
        private readonly IDbcStore dbcStore;
        private readonly ISpellStore spellStore;
        private readonly IParameterFactory parameterFactory;
        private readonly IWaypointProcessor waypointProcessor;
        private readonly IChatEmoteSoundProcessor chatProcessor;
        private readonly IRandomMovementDetector randomMovementDetector;
        private readonly HighLevelUpdateDump highLevelUpdateDump;
        private readonly StringBuilder sb;
        private readonly TextWriter writer;
        private DateTime? lastTime;
        private readonly Dictionary<UniversalGuid, int> guids = new();
        private int currentShortGuid;
        private UniversalGuid? playerGuid;
        private readonly Dictionary<UniversalGuid, Dictionary<int, uint>> auras = new();
        private readonly Dictionary<uint, Dictionary<uint, string>> gossips = new();

        private readonly Dictionary<UniversalGuid, Entity> entities = new();

        private IParameter<long> unitFlagsParameter;
        private IParameter<long> unitFlags2Parameter;
        private IParameter<long> factionParameter;
        private IParameter<long> emoteParameter;
        private IParameter<long> npcFlagsParameter;
        private IParameter<long> gameobjectBytes1Parameter;
        private IParameter<long>[] unitBytesParameters = new IParameter<long>[3];
        
        public StoryTellerDumper(IDatabaseProvider databaseProvider, 
            IDbcStore dbcStore,
            ISpellStore spellStore,
            IParameterFactory parameterFactory,
            IWaypointProcessor waypointProcessor,
            IChatEmoteSoundProcessor chatProcessor,
            IRandomMovementDetector randomMovementDetector,
            HighLevelUpdateDump highLevelUpdateDump) : base(waypointProcessor, chatProcessor, randomMovementDetector)
        {
            this.databaseProvider = databaseProvider;
            this.dbcStore = dbcStore;
            this.spellStore = spellStore;
            this.parameterFactory = parameterFactory;
            this.waypointProcessor = waypointProcessor;
            this.chatProcessor = chatProcessor;
            this.randomMovementDetector = randomMovementDetector;
            this.highLevelUpdateDump = highLevelUpdateDump;
            unitFlagsParameter = parameterFactory.Factory("UnitFlagParameter");
            unitFlags2Parameter = parameterFactory.Factory("UnitFlags2Parameter");
            factionParameter = parameterFactory.Factory("FactionParameter");
            emoteParameter = parameterFactory.Factory("EmoteParameter");
            npcFlagsParameter = parameterFactory.Factory("NpcFlagParameter");
            gameobjectBytes1Parameter = parameterFactory.Factory("GameobjectBytes1Parameter");
            unitBytesParameters[0] = parameterFactory.Factory("UnitBytes0Parameter");
            unitBytesParameters[1] = parameterFactory.Factory("UnitBytes1Parameter");
            unitBytesParameters[2] = parameterFactory.Factory("UnitBytes2Parameter");
            
            this.sb = new();
            this.writer = new StringWriter(sb);
        }

        private string? pendingAppend;

        private void SetAppendOnNext(string? line)
        {
            pendingAppend = line;
        }
        
        private void AppendLine(PacketBase packet, string text, bool skipPacketNumber = false)
        {
            if (pendingAppend != null)
            {
                var pending = pendingAppend;
                pendingAppend = null;
                AppendLine(packet, pending);
            }
            
            if (!lastTime.HasValue)
                lastTime = packet.Time.ToDateTime();
            else
            {
                TimeSpan diff = packet.Time.ToDateTime().Subtract(lastTime.Value);
                if (diff.TotalMilliseconds > 130)
                {
                    writer.WriteLine();
                    writer.WriteLine("After " + diff.TotalMilliseconds + " ms");
                    lastTime = packet.Time.ToDateTime();
                }
            }
            if (!skipPacketNumber)
                writer.WriteLine("   " + text + " ["+packet.Number+"]");
            else
                writer.WriteLine("   " + text);
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
                if (playerGuid?.Equals(guid) ?? false)
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
            if (playerGuid != null && playerGuid.Equals(guid))
                return "Player";

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
            AppendLine(basePacket, NiceGuid(packet.Sender) + " says: `" + packet.Text + "`"
                                   + (emote.HasValue ? " with emote " + GetStringFromDbc(dbcStore.EmoteStore, emote.Value) : "")
                                   + (sound.HasValue ? " with sound " + GetStringFromDbc(dbcStore.SoundStore, (int)sound.Value) : ""));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayerLogin packet)
        {
            playerGuid = packet.PlayerGuid;
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketEmote packet)
        {
            if (chatProcessor.IsEmoteForChat(basePacket))
                return false;
            AppendLine(basePacket, NiceGuid(packet.Sender) + " plays emote: " + GetStringFromDbc(dbcStore.EmoteStore, packet.Emote));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlaySound packet)
        {
            if (chatProcessor.IsSoundForChat(basePacket))
                return false;
            AppendLine(basePacket, NiceGuid(packet.Source) + " plays sound: " + GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayMusic packet)
        {
            AppendLine(basePacket, $"music: {packet.Music} plays");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayObjectSound packet)
        {
            if (chatProcessor.IsSoundForChat(basePacket))
                return false;
            AppendLine(basePacket, $"{NiceGuid(packet.Source)} plays object sound: {GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound)} to {NiceGuid(packet.Target)}");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            if (!auras.ContainsKey(packet.Unit))
                auras[packet.Unit] = new Dictionary<int, uint>();
            AppendLine(basePacket, NiceGuid(packet.Unit) + " auras update:");
            foreach (var update in packet.Updates)
            {
                if (update.Remove && auras[packet.Unit].ContainsKey(update.Slot))
                {
                    AppendLine(basePacket,
                        "    removed aura: " + GetSpellName(auras[packet.Unit][update.Slot]));
                    auras[packet.Unit].Remove(update.Slot);
                }
                else if (!update.Remove)
                {
                    auras[packet.Unit][update.Slot] = update.Spell;
                    AppendLine(basePacket,
                        "    applied aura: " + GetSpellName(auras[packet.Unit][update.Slot]));
                }
            }
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellGo packet)
        {
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

            AppendLine(basePacket, NiceGuid(packet.Data.Caster) + " casts: " + GetSpellName(packet.Data.Spell) + " " + targetLine);
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
                AppendLine(basePacket, "Player choose option: " + gossips[packet.MenuId][packet.OptionId]);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipHello packet)
        {
            AppendLine(basePacket, "Player talks to: " + NiceGuid(packet.GossipSource));
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
        
        protected override bool Process(PacketBase basePacket, PacketQuestGiverAcceptQuest packet)
        {
            AppendLine(basePacket, "Player accepts quest: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestGiverQuestComplete packet)
        {
            AppendLine(basePacket, "Player rewards quest: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestComplete packet)
        {
            AppendLine(basePacket, "Quest completed: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestAddKillCredit packet)
        {
            AppendLine(basePacket, $"Added kill credit {packet.KillCredit} for quest " + GetQuestName(packet.QuestId) + $" ({packet.Count}/{packet.RequiredCount})");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestFailed packet)
        {
            AppendLine(basePacket, "Quest failed: " + GetQuestName(packet.QuestId));
            return base.Process(basePacket, packet);
        }
        
        protected override bool Process(PacketBase basePacket, PacketSpellClick packet)
        {
            AppendLine(basePacket, "Player spell click on: " + NiceGuid(packet.Target));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            if (randomMovementDetector.RandomMovementPacketRatio(packet.Mover) > 0.90f)
                return false;

            bool lastPathSegmentHadOrientation = false;
            StringBuilder sb = new StringBuilder();

            if (packet.Flags.HasFlag(UniversalSplineFlag.TransportEnter))
                sb.Append("enters " +
                          (packet.TransportGuid.Type == UniversalHighGuid.Vehicle ? "vehicle" : "transport") + " " +
                          NiceGuid(packet.TransportGuid) + " on seat " + packet.VehicleSeat);
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
                sb.AppendLine($"goes by waypoints [{path.TotalMoveTime} ms]: {{");
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
            }

            var skipOrientation = lastPathSegmentHadOrientation &&
                                  packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookOrientation;
            if (packet.FacingCase != PacketMonsterMove.FacingOneofCase.None && !skipOrientation)
            {
                if (sb.Length > 0)
                    sb.Append("\n    then ");
                if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookOrientation)
                    sb.Append("looks at " + packet.LookOrientation);
                else if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookTarget)
                    sb.Append("looks at " + NiceGuid(packet.LookTarget.Target));
                else if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookPosition)
                    sb.Append($"looks at ({packet.LookPosition.X}, {packet.LookPosition.Y}, {packet.LookPosition.Z})");   
            }

            if (sb.Length == 0)
                sb.Append("stops");

            var randomRatio = randomMovementDetector.RandomMovementPacketRatio(packet.Mover);
            AppendLine(basePacket, NiceGuid(packet.Mover) + $" [random movement chance: {randomRatio*100:0}%] " + sb);
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

            AppendLine(basePacket, sb.ToString());
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketClientUseItem packet)
        {
            AppendLine(basePacket, "Player uses item in backpack and cast spell " + GetSpellName(packet.SpellId));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketOneShotAnimKit packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Unit) + " plays one shot anim kit " + packet.AnimKit);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketClientAreaTrigger packet)
        {
            AppendLine(basePacket, "Player " + (packet.Enter ? "enters" : "leaves") + " clientside area trigger " + packet.AreaTrigger);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSetAnimKit packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Unit) + " sets anim kit " + packet.AnimKit);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var destroyed in packet.Destroyed)
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed.Guid);
                AppendLine(basePacket, "Destroyed " + NiceGuid(destroyed.Guid));
            }

            foreach (var destroyed in packet.OutOfRange)
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed.Guid);
                AppendLine(basePacket, "Out of range: " + NiceGuid(destroyed.Guid));
            }

            foreach (var created in packet.Created)
            {
                if (created.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                var entity = new Entity();
                foreach (var pair in created.Values.Ints)
                    entity.Ints[pair.Key] = RemoveUselessFlag(pair.Key, pair.Value);
                foreach (var pair in created.Values.Floats)
                    entity.Floats[pair.Key] = pair.Value;
                foreach (var pair in created.Values.Guids)
                    entity.Guids[pair.Key] = pair.Value;
                SetAppendOnNext("Created " + NiceGuid(created.Guid) + " at " + VecToString(created.Movement?.Position ?? created.Stationary?.Position, created.Movement?.Orientation ?? created.Stationary?.Orientation));
                PrintValues(basePacket, created.Guid, created.Values, null, false);
                SetAppendOnNext(null);
            }
            
            foreach (var updated in packet.Updated)
            {
                if (updated.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.TryGetValue(updated.Guid, out var entity);
                SetAppendOnNext("Updated " + NiceGuid(updated.Guid));
                PrintValues(basePacket, updated.Guid, updated.Values, entity, true);
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

        private void PrintValues(PacketBase basePacket, UniversalGuid guid, UpdateValues values, Entity? entity, bool isUpdate)
        {
            var newEntity = entity ?? new Entity();
            if (entity == null)
                entities[guid] = newEntity;
            
            foreach (var val in values.Ints)
            {
                var newValue = RemoveUselessFlag(val.Key, val.Value);
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                {
                    newEntity.Ints[val.Key] = newValue;
                    continue;
                }

                if (entity == null || !entity.Ints.TryGetValue(val.Key, out var intValue))
                {
                    var param = GetPrettyParameter(val.Key);
                    var stringValue = param == null ? "" : $" [{param.ToString(newValue)}]";
                    AppendLine(basePacket, $"     {val.Key} = {newValue}{stringValue}", true);
                }
                else if (intValue != newValue)
                {
                    var param = GetPrettyParameter(val.Key);
                    var oldStringValue = param == null ? "" : $" [{param.ToString(intValue)}]";
                    var newStringValue = param == null ? "" : $" [{param.ToString(newValue)}]";
                    var change = TryGenerateFlagsDiff(param, val.Key, intValue, newValue);
                    if (change == null)
                        AppendLine(basePacket, $"     {val.Key} = {newValue}{newStringValue} (old: {intValue}{oldStringValue})", true);
                    else
                        AppendLine(basePacket, $"     {val.Key} = {newValue} (old: {intValue}) {change}", true);

                    foreach (var extra in highLevelUpdateDump.Produce(val.Key, (uint)intValue, (uint)newValue))
                        AppendLine(basePacket, $"       -> {extra}", true);   
                    
                }
                newEntity.Ints[val.Key] = newValue;
            }
            
            foreach (var val in values.Floats)
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                {
                    newEntity.Floats[val.Key] = val.Value;
                    continue;
                }
                
                if (entity == null || !entity.Floats.TryGetValue(val.Key, out var intValue))
                    AppendLine(basePacket, $"     {val.Key} = {val.Value}", true);
                else if (Math.Abs(intValue - val.Value) > 0.01f)
                    AppendLine(basePacket, $"     {val.Key} = {val.Value} (old: {intValue})", true);
                newEntity.Floats[val.Key] = val.Value;
            }
            
            foreach (var val in values.Guids)
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                {
                    newEntity.Guids[val.Key] = val.Value;
                    continue;
                }
                
                if (entity == null || !entity.Guids.TryGetValue(val.Key, out var intValue))
                    AppendLine(basePacket, $"     {val.Key} = {val.Value}", true);
                else if (!intValue.Equals(val.Value))
                    AppendLine(basePacket, $"     {val.Key} = {val.Value} (old: {intValue})", true);
                newEntity.Guids[val.Key] = val.Value;
            }
        }

        private IParameter<long>? GetPrettyParameter(string field)
        {
            switch (field)
            {
                case "UNIT_FIELD_FACTIONTEMPLATE":
                    return factionParameter;
                case "UNIT_FIELD_FLAGS":
                    return unitFlagsParameter;
                case "UNIT_FIELD_FLAGS_2":
                    return unitFlags2Parameter;
                case "UNIT_NPC_EMOTESTATE":
                    return emoteParameter;
                case "UNIT_NPC_FLAGS":
                    return npcFlagsParameter;
                case "UNIT_FIELD_BYTES_0":
                    return unitBytesParameters[0];
                case "UNIT_FIELD_BYTES_1":
                    return unitBytesParameters[1];
                case "UNIT_FIELD_BYTES_2":
                    return unitBytesParameters[2];
                case "GAMEOBJECT_BYTES_1":
                    return gameobjectBytes1Parameter;
            }

            return null;
        }

        private long RemoveUselessFlag(string field, long value)
        {
            if (field == "UNIT_FIELD_FLAGS")
            {
                value = value & ~ (uint)GameDefines.UnitFlags.ServerSideControlled;
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
            return sb.ToString();
        }
    }
}