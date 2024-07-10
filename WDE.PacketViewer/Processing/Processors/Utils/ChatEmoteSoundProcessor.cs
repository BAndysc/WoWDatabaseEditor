using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [UniqueProvider]
    public interface IChatEmoteSoundProcessor : IPacketProcessor<bool>, IPerFileStateProcessor
    {
        /**
         * Returns an emote id if processor thinks the chat packet has associated emote packet
         * returns null if processor thinks that chat has no emote
         */
        int? GetEmoteForChat(PacketBase chat);
        
        /**
         * Returns a sound id if processor thinks the chat packet has associated sound packet
         * returns null if processor thinks that chat has no sound
         */
        uint? GetSoundForChat(PacketBase chat);

        /**
         * Returns true if processor thinks this emote packet is sent with other chat packet
         */
        bool IsEmoteForChat(PacketBase emote);
        bool IsEmoteForChat(int packetNumber);
        
        int? GetChatPacketForEmote(int emoteNumber);
        
        /**
         * Returns true if processor thinks this sound (play sound or play object sound) packet is sent with other chat packet
         */
        bool IsSoundForChat(PacketBase sound);

        /**
         * Returns text from the chat packet, translated if the processor support it
         */
        string GetTextForChar(PacketBase chat);
    }
    
    [AutoRegister]
    public class ChatEmoteSoundProcessor : PacketProcessor<bool>, IChatEmoteSoundProcessor, INeedToPostProcess
    {
        private readonly IDatabaseProvider databaseProvider;
        private readonly IParsingSettings parsingSettings;

        private class State
        {
            public (PacketBase packet, PacketChat chat)? LastChat;
            public (PacketBase packet, int emote)? LastEmote;
            public (PacketBase packet, uint sound)? LastSound;
        }
        
        private readonly Dictionary<UniversalGuid, State> perGuidState = new();

        private readonly Dictionary<int, int> emotePacketIdToChatPacketId = new();
        private readonly Dictionary<int, int> soundPacketIdToChatPacketId = new();
        
        private readonly Dictionary<int, int> chatPacketIdToEmote = new();
        private readonly Dictionary<int, uint> chatPacketIdToSound = new();
        
        private readonly Dictionary<int, PacketChat> numberToChat = new();
        private Dictionary<int, string>? numberToTranslatedText;

        public ChatEmoteSoundProcessor(IDatabaseProvider databaseProvider, IParsingSettings parsingSettings)
        {
            this.databaseProvider = databaseProvider;
            this.parsingSettings = parsingSettings;
        }
        
        private State Get(UniversalGuid guid)
        {
            if (perGuidState.TryGetValue(guid, out var state))
                return state;
            state = new();
            perGuidState[guid] = state;
            return state;
        }

        private bool HasJustHappened(PacketBase? a, PacketBase? b)
        {
            if (a == null || b == null)
                return false;
            
            var timeA = a.Value.Time.ToDateTime();
            var timeB = b.Value.Time.ToDateTime();
            return Math.Abs((timeB - timeA).TotalMilliseconds) < 200;
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            var state = Get(packet.Sender);
            state.LastChat = (basePacket, packet);

            if (HasJustHappened(state.LastEmote?.packet, basePacket))
            {
                chatPacketIdToEmote[basePacket.Number] = state.LastEmote!.Value.emote;
                emotePacketIdToChatPacketId[state.LastEmote!.Value.packet.Number] = basePacket.Number;
            }
            
            if (HasJustHappened(state.LastSound?.packet, basePacket))
            {
                chatPacketIdToSound[basePacket.Number] = state.LastSound!.Value.sound;
                soundPacketIdToChatPacketId[state.LastSound!.Value.packet.Number] = basePacket.Number;
            }

            numberToChat[basePacket.Number] = packet;
            return true;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlayObjectSound packet)
        {
            var state = Get(packet.Source);
            if (state.LastChat.HasValue &&
                !chatPacketIdToSound.ContainsKey(state.LastChat.Value.packet.Number) &&
                HasJustHappened(state.LastChat?.packet, basePacket))
            {
                var chatId = state.LastChat!.Value.packet.Number;
                chatPacketIdToSound[chatId] = packet.Sound;
                soundPacketIdToChatPacketId[basePacket.Number] = chatId;
            }
            else
                Get(packet.Source).LastSound = (basePacket, packet.Sound);
            return true;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySound packet)
        {
            var state = Get(packet.Source);
            if (state.LastChat.HasValue &&
                !chatPacketIdToSound.ContainsKey(state.LastChat.Value.packet.Number) &&
                HasJustHappened(state.LastChat?.packet, basePacket))
            {
                var chatId = state.LastChat!.Value.packet.Number;
                chatPacketIdToSound[chatId] = packet.Sound;
                soundPacketIdToChatPacketId[basePacket.Number] = chatId;
            }
            else
                Get(packet.Source).LastSound = (basePacket, packet.Sound);
            return true;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketEmote packet)
        {
            var state = Get(packet.Sender);
            if (state.LastChat.HasValue &&
                !chatPacketIdToEmote.ContainsKey(state.LastChat.Value.packet.Number) &&
                HasJustHappened(state.LastChat?.packet, basePacket))
            {
                var chatId = state.LastChat!.Value.packet.Number;
                chatPacketIdToEmote[chatId] = packet.Emote;
                emotePacketIdToChatPacketId[basePacket.Number] = chatId;
            }
            else
                state.LastEmote = (basePacket, packet.Emote);
            return true;
        }

        public int? GetEmoteForChat(PacketBase chat)
        {
            if (chatPacketIdToEmote.TryGetValue(chat.Number, out var emote))
                return emote;
            return null;
        }

        public uint? GetSoundForChat(PacketBase chat)
        {
            if (chatPacketIdToSound.TryGetValue(chat.Number, out var sound))
                return sound;
            return null;
        }

        public bool IsEmoteForChat(PacketBase emote) => IsEmoteForChat(emote.Number);

        public bool IsEmoteForChat(int packetNumber)
        {
            return emotePacketIdToChatPacketId.ContainsKey(packetNumber);
        }

        public int? GetChatPacketForEmote(int emoteNumber)
        {
            if (emotePacketIdToChatPacketId.TryGetValue(emoteNumber, out var packetId))
                return packetId;
            return null;
        }

        public bool IsSoundForChat(PacketBase sound)
        {
            return soundPacketIdToChatPacketId.ContainsKey(sound.Number);
        }

        public async Task PostProcess()
        {
            if (!parsingSettings.TranslateChatToEnglish)
                return;
            
            numberToTranslatedText = new();
            
            foreach (var pair in numberToChat)
            {
                var locale = await databaseProvider.GetBroadcastTextLocaleByTextAsync(pair.Value.Text.ToString() ?? "");
                if (locale == null)
                    continue;

                var broadcastText = await databaseProvider.GetBroadcastTextByIdAsync(locale.Id);

                if (broadcastText == null)
                    continue;

                var text = pair.Value.Text.ToString() == locale.Text ? broadcastText.Text : broadcastText.Text1;

                if (text == null)
                    continue;

                numberToTranslatedText[pair.Key] = text;
            }
        }

        public string GetTextForChar(PacketBase chat)
        {
            if (numberToTranslatedText != null &&
                numberToTranslatedText.TryGetValue(chat.Number, out var englishText))
                return englishText;
            
            if (numberToChat.TryGetValue(chat.Number, out var chatPacket))
                return chatPacket.Text.ToString() ?? "";

            return "";
        }

        public void ClearAllState()
        {
            numberToTranslatedText?.Clear();
            numberToChat.Clear();
            perGuidState.Clear();
            emotePacketIdToChatPacketId.Clear();
            soundPacketIdToChatPacketId.Clear();
            chatPacketIdToEmote.Clear();
            soundPacketIdToChatPacketId.Clear();
        }
    }
}