using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [AutoRegister]
    public class QuestPacketRangesProcessor : PacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IDatabaseProvider databaseProvider;

        public class QuestState
        {
            public int Accepted = -1;
            public int Completed = -1;
            public int Rewarded = -1;
            public int Failed = -1;
            public DateTime AcceptedTime;
            public DateTime RewardedTime;
        }
        
        private Dictionary<uint, QuestState> states = new();

        private QuestState Get(uint entry)
        {
            if (states.TryGetValue(entry, out var state))
                return state;
            states[entry] = new();
            return states[entry];
        }

        public QuestPacketRangesProcessor(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestComplete packet)
        {
            Get(packet.QuestId).Completed = basePacket.Number;
            return true;
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestFailed packet) 
        {
            Get(packet.QuestId).Failed = basePacket.Number;
            return true;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverQuestComplete packet)
        {
            Get(packet.QuestId).Rewarded = basePacket.Number;
            Get(packet.QuestId).RewardedTime = basePacket.Time.ToDateTime();
            return true;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverAcceptQuest packet)
        {
            Get(packet.QuestId).Accepted = basePacket.Number;
            Get(packet.QuestId).AcceptedTime = basePacket.Time.ToDateTime();
            return true;
        }

        public async Task<string> Generate()
        {
            StringBuilder sb = new();
            foreach (var state in states)
            {
                var template = await databaseProvider.GetQuestTemplate(state.Key);
                sb.AppendLine(template == null ? $"Quest {state.Key}:" : $"{template.Name} ({template.Entry}):");
                sb.AppendLine("  Accepted: " + (state.Value.Accepted == -1 ? "(not in sniff)" : state.Value.Accepted));
                if (state.Value.Failed != -1)
                    sb.AppendLine("  Failed: " + state.Value.Failed);
                sb.AppendLine("  Completed: " + (state.Value.Completed == -1 ? "(not in sniff)" : state.Value.Completed));
                sb.AppendLine("  Rewarded: " + (state.Value.Rewarded == -1 ? "(not in sniff)" : state.Value.Rewarded));

                if (state.Value.Accepted != -1 && state.Value.Rewarded != -1)
                {
                    var took = (state.Value.RewardedTime - state.Value.AcceptedTime);
                    sb.Append("  Took: ");
                    if (took.Hours > 0)
                        sb.Append($" {took.Hours} hours");
                    if (took.Minutes > 0)
                        sb.Append($" {took.Minutes} mins");
                    if (took.Seconds > 0)
                        sb.Append($" {took.Seconds} secs");
                    sb.AppendLine();
                    sb.AppendLine(
                        $"     // packet.id >= {state.Value.Accepted} and packet.id <= {state.Value.Rewarded}");
                }
                sb.AppendLine();
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}