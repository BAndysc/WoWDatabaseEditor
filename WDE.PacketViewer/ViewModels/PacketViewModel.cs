using System;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public class PacketViewModel
    {
        public int Id { get; }
        public string Opcode { get; }
        public string Text { get; }
        public PacketHolder Packet { get; }
        public DateTime Time { get; }
        public int Diff { get; set;  }
        public uint Entry { get; }
        
        public PacketViewModel(PacketHolder packet, DateTime time, uint entry)
        {
            Id = packet.BaseData.Number;
            Packet = packet;
            Time = time;
            Opcode = packet.BaseData.Opcode;
            Text = packet.BaseData.StringData;
            Entry = entry;
        }
    }
}