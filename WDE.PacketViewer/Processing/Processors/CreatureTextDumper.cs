using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.PacketViewer.Processing.Runners;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureTextDumper : CompoundProcessor<bool, IChatEmoteSoundProcessor>, IPacketTextDumper, IPerFileStateProcessor
    {
        private class TextEntry : IEquatable<TextEntry>
        {
            public byte GroupId;
            public byte Id;
            public float Probability;
            public CreatureTextRange Range;
            public uint Duration;
            public readonly CreatureTextType Type;
            public readonly uint Language;
            public readonly uint Emote;
            public readonly uint Sound;
            public readonly string Text;
            public bool IsInSniffText { get; set; }
            public bool IsInDatabaseText { get; set; }
            public uint BroadcastTextId { get; set; }

            public string? Comment;

            public TextEntry(string text, CreatureTextType type, uint language, uint emote, uint sound)
            {
                Text = text;
                Type = type;
                Language = language;
                Emote = emote;
                Sound = sound;
                Probability = 100;
                Range = CreatureTextRange.Normal;
                IsInSniffText = true;
                IsInDatabaseText = false;
            }
            
            public TextEntry(TextEntry other, string text)
            {
                Text = text;
                GroupId = other.GroupId;
                Id = other.Id;
                Duration = other.Duration;
                Type = other.Type;
                Language = other.Language;
                Emote = other.Emote;
                Sound = other.Sound;
                Probability = other.Probability;
                Range = other.Range;
                Comment = other.Comment;
                BroadcastTextId = other.BroadcastTextId;
                IsInSniffText = other.IsInSniffText;
                IsInDatabaseText = other.IsInDatabaseText;
            }

            public TextEntry(ICreatureText text)
            {
                IsInSniffText = false;
                IsInDatabaseText = true;
                Text = text.Text ?? "";
                Type = text.Type;
                Language = text.Language;
                Emote = text.Emote;
                Sound = text.Sound;
                GroupId = text.GroupId;
                Probability = text.Probability;
                Duration = text.Duration;
                Range = text.TextRange;
                Id = text.Id;
                BroadcastTextId = text.BroadcastTextId;
                Comment = text.Comment;
            }

            public bool Equals(TextEntry? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Type == other.Type && Language == other.Language && Emote == other.Emote && Sound == other.Sound && Text == other.Text;
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TextEntry)obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int)Type, Language, Emote, Sound, Text);
            }
        }
        
        private class State
        {
            public HashSet<TextEntry> texts = new();
        }
        
        private readonly IChatEmoteSoundProcessor chatEmoteSoundProcessor;
        private readonly ICachedDatabaseProvider databaseProvider;
        private readonly IQueryGenerator<ICreatureText> queryGenerator;
        private readonly bool asDiff;

        private readonly Dictionary<uint, State> perEntryState = new();

        public CreatureTextDumper(IChatEmoteSoundProcessor chatEmoteSoundProcessor, 
            ICachedDatabaseProvider databaseProvider,
            IQueryGenerator<ICreatureText> queryGenerator,
            bool asDiff) : base (chatEmoteSoundProcessor)
        {
            this.chatEmoteSoundProcessor = chatEmoteSoundProcessor;
            this.databaseProvider = databaseProvider;
            this.queryGenerator = queryGenerator;
            this.asDiff = asDiff;
        }

        private State GetState(UniversalGuid guid)
        {
            if (perEntryState.TryGetValue(guid.Entry, out var state))
                return state;
            return perEntryState[guid.Entry] = new();
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            if (packet.Sender.Type != UniversalHighGuid.Creature &&
                packet.Sender.Type != UniversalHighGuid.Vehicle &&
                packet.Sender.Type != UniversalHighGuid.GameObject)
                return false;
            
            var emote = chatEmoteSoundProcessor.GetEmoteForChat(basePacket);
            var sound = chatEmoteSoundProcessor.GetSoundForChat(basePacket);
            var text = chatEmoteSoundProcessor.GetTextForChar(basePacket);

            var state = GetState(packet.Sender);
            
            var entry = new TextEntry(text, (CreatureTextType)packet.Type, (uint)packet.Language, (uint)(emote ?? 0), sound ?? 0)
            {
                GroupId = (byte)state.texts.Count,
                Id = 0,
            };
            return state.texts.Add(entry);
        }

        public async Task<string> Generate()
        {
            var trans = Queries.BeginTransaction(DataDatabaseType.World);
            if (!asDiff)
                trans.Comment("Warning!! This SQL will override current texts");
            foreach (var entry in perEntryState)
            {
                int maxId = -1;
                if (asDiff)
                {
                    var existing = await databaseProvider.GetCreatureTextsByEntryAsync(entry.Key);
                    foreach (var text in existing)
                    {
                        if (text.Text == null)
                            continue;
                        var databaseEntry = new TextEntry(text);
                        if (entry.Value.texts.TryGetValue(databaseEntry, out var sniffEntry))
                        {
                            entry.Value.texts.Remove(sniffEntry);
                            databaseEntry.IsInSniffText = true;
                        }
                        entry.Value.texts.Add(databaseEntry);
                        maxId = Math.Max(maxId, text.GroupId);
                    }    
                }

                foreach (var sniffText in entry.Value.texts.Where(t => t.IsInSniffText && !t.IsInDatabaseText))
                {
                    if (sniffText.BroadcastTextId == 0)
                        sniffText.BroadcastTextId =
                            (await databaseProvider.GetBroadcastTextByTextAsync(sniffText.Text))?.Id ?? 0;
                    sniffText.GroupId = (byte)(++maxId);
                }
                
                var template = databaseProvider.GetCachedCreatureTemplate(entry.Key);
                if (template != null)
                    trans.Comment(template.Name);

                trans.Add(queryGenerator.Delete(new AbstractCreatureText() { CreatureId = entry.Key }));
                trans.Add(queryGenerator.BulkInsert(entry.Value.texts
                    .OrderBy(t => t.GroupId)
                    .ThenBy(t => t.Id)
                    .Select(text => new AbstractCreatureText()
                    {
                        CreatureId = entry.Key,
                        GroupId = text.GroupId,
                        Id = text.Id,
                        Text = text.Text,
                        Type = text.Type,
                        Language = (byte)text.Language,
                        Probability = text.Probability,
                        Duration = text.Duration,
                        TextRange = text.Range,
                        Emote = text.Emote,
                        Sound = text.Sound,
                        BroadcastTextId = text.BroadcastTextId,
                        Comment = text.Comment ?? template?.Name ?? "",
                        __comment = !text.IsInSniffText ? "not in sniff" : null
                    }).ToList()));
                trans.BlankLine();
            }
            
            return trans.Close().QueryString;
        }

        public void ClearAllState()
        {
            chatEmoteSoundProcessor.ClearAllState();
        }
    }
}