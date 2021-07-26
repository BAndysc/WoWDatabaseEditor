using System;
using System.Collections.Generic;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class SplitUpdateProcessor : PacketProcessor<IEnumerable<PacketHolder>?>
    {
        private string GenerateText(PacketBase basePacket, string text)
        {
            int indexOfFirstLine = basePacket.StringData.IndexOf("\n", StringComparison.Ordinal);
            if (indexOfFirstLine == -1)
                return text;
            int indexOfSecondLine = basePacket.StringData.IndexOf("\n", indexOfFirstLine + 1, StringComparison.Ordinal);
            if (indexOfSecondLine == -1)
                return text;
            int indexOfThirdLine = basePacket.StringData.IndexOf("\n", indexOfSecondLine + 1, StringComparison.Ordinal);
            if (indexOfThirdLine == -1)
                return text;
            return basePacket.StringData.Substring(0, indexOfThirdLine + 1) +
                   text;
        }
        
        protected override IEnumerable<PacketHolder>? Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var destroyed in packet.Destroyed)
            {
                var fake = new PacketUpdateObject();
                fake.Destroyed.Add(destroyed);
                yield return new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, destroyed.Text)},
                    UpdateObject = fake
                };
            }
            
            foreach (var outOfRange in packet.OutOfRange)
            {
                var fake = new PacketUpdateObject();
                fake.OutOfRange.Add(outOfRange);
                yield return new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, outOfRange.Text)},
                    UpdateObject = fake
                };
            }
            
            foreach (var created in packet.Created)
            {
                var fake = new PacketUpdateObject();
                fake.Created.Add(created);
                yield return new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, created.Text)},
                    UpdateObject = fake
                };
            }
            
            foreach (var updated in packet.Updated)
            {
                var fake = new PacketUpdateObject();
                fake.Updated.Add(updated);
                yield return new PacketHolder()
                {
                    BaseData = new PacketBase(basePacket){StringData = GenerateText(basePacket, updated.Text)},
                    UpdateObject = fake
                };
            }
        }
    }
}