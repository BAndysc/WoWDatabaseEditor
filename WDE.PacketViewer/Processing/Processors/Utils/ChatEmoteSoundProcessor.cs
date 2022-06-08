using System;
using System.Collections.Generic;
using System.Linq;
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
    }
    
    [AutoRegister]
    public class ChatEmoteSoundProcessor : PacketProcessor<bool>, IChatEmoteSoundProcessor
    {
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
            
            var timeA = a.Time.ToDateTime();
            var timeB = b.Time.ToDateTime();
            return Math.Abs((timeB - timeA).TotalMilliseconds) < 200;
        }
        
        protected override bool Process(PacketBase basePacket, PacketChat packet)
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
            return true;
        }

        protected override bool Process(PacketBase basePacket, PacketPlayObjectSound packet)
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

        protected override bool Process(PacketBase basePacket, PacketPlaySound packet)
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

        protected override bool Process(PacketBase basePacket, PacketEmote packet)
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

        public void ClearAllState()
        {
            perGuidState.Clear();
            emotePacketIdToChatPacketId.Clear();
            soundPacketIdToChatPacketId.Clear();
            chatPacketIdToEmote.Clear();
            soundPacketIdToChatPacketId.Clear();
        }
    }
}