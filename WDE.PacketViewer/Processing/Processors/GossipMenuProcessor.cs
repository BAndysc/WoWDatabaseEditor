using System;
using WDE.MVVM.Observable;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class GossipMenuProcessor : PacketProcessor<Unit>
    {
        protected override Unit Process(PacketBase packetBaseData, PacketGossipHello packet)
        {
            Console.WriteLine("Talking to " + packet.GossipSource.Entry);
            return default;
        }

        protected override Unit Process(PacketBase packetBaseData, PacketGossipMessage packet)
        {
            Console.WriteLine("Menu id: " + packet.MenuId);
            Console.WriteLine("Text id: " + packet.TextId);

            foreach (var option in packet.Options)
            {
                Console.WriteLine($"  {option.OptionIndex}. {option.Text}");
            }

            return default;
        }

        protected override Unit Process(PacketBase packetBaseData, PacketGossipSelect packet)
        {
            Console.WriteLine("Pick: " + packet.MenuId + " / " + packet.OptionId);
            return default;
        }
    }
}