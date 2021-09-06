using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureTextDumper : PacketProcessor<bool>, ITwoStepPacketBoolProcessor, IPacketTextDumper
    {
        private class State
        {
            public Dictionary<string, (int groupdId, int type, int language, int emote, uint sound)> texts = new();
        }
        
        private readonly IChatEmoteSoundProcessor chatEmoteSoundProcessor;
        private readonly IDatabaseProvider databaseProvider;

        private readonly Dictionary<uint, State> perEntryState = new();

        public CreatureTextDumper(IChatEmoteSoundProcessor chatEmoteSoundProcessor, IDatabaseProvider databaseProvider)
        {
            this.chatEmoteSoundProcessor = chatEmoteSoundProcessor;
            this.databaseProvider = databaseProvider;
        }

        private State GetState(UniversalGuid guid)
        {
            if (perEntryState.TryGetValue(guid.Entry, out var state))
                return state;
            return perEntryState[guid.Entry] = new();
        }
        
        protected override bool Process(PacketBase basePacket, PacketChat packet)
        {
            if (packet.Sender.Type != UniversalHighGuid.Creature &&
                packet.Sender.Type != UniversalHighGuid.Vehicle &&
                packet.Sender.Type != UniversalHighGuid.GameObject)
                return false;
            
            var emote = chatEmoteSoundProcessor.GetEmoteForChat(basePacket);
            var sound = chatEmoteSoundProcessor.GetSoundForChat(basePacket);

            var state = GetState(packet.Sender);

            if (state.texts.ContainsKey(packet.Text))
                return false;

            state.texts[packet.Text] = (state.texts.Count, packet.Type, packet.Language, emote ?? 0, sound ?? 0);
            return true;
        }

        public async Task<string> Generate()
        {
            var trans = Queries.BeginTransaction();
            trans.Comment("Warning!! This SQL will override current texts");
            foreach (var entry in perEntryState)
            {
                var template = databaseProvider.GetCreatureTemplate(entry.Key);
                if (template != null)
                    trans.Comment(template.Name);
                trans.Table("creature_text")
                    .Where(row => row.Column<uint>("CreatureID") == entry.Key)
                    .Delete();
                trans.Table("creature_text")
                    .BulkInsert(entry.Value.texts
                        .Select(text => new
                        {
                            CreatureID = entry.Key,
                            GroupID = text.Value.groupdId,
                            ID = 0,
                            Text = text.Key,
                            Type = text.Value.type,
                            Language = text.Value.language,
                            Probability = 100,
                            Emote = text.Value.emote,
                            Sound = text.Value.sound,
                            BroadcastTextId = databaseProvider.GetBroadcastTextByText(text.Key)?.Id ?? 0
                        }));
                trans.BlankLine();
            }
            
            return trans.Close().QueryString;
        }

        public bool PreProcess(PacketHolder packet)
        {
            return chatEmoteSoundProcessor.Process(packet);
        }
    }
}