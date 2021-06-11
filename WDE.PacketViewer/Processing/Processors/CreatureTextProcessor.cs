using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureTextProcessor : PacketProcessor<object[]?>
    {
        private readonly IDatabaseProvider databaseProvider;
        public static string[] Columns => new[] {"CreatureID", "Text", "Type", "Language", "BroadcastTextId"};
        private HashSet<uint> keys = new();

        public CreatureTextProcessor(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        public ISet<uint> Keys => keys;
        
        protected override object[]? Process(PacketBase basePacket, PacketChat packet)
        {
            if (packet.Sender.Entry == 0)
                return null;

            keys.Add(packet.Sender.Entry);

            return new object[]
            {
                (uint)packet.Sender.Entry,
                packet.Text,
                (long)packet.Type,
                (long)packet.Language,
                (long)(databaseProvider.GetBroadcastTextByText(packet.Text)?.Id ?? 0)
            };
        }
    }
}