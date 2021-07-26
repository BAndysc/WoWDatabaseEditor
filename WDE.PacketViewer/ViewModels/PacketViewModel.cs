using System;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public class PacketViewModel
    {
        public int Id => Packet.BaseData.Number;
        public string Opcode => Packet.BaseData.Opcode;
        public string Text => Packet.BaseData.StringData;
        public PacketHolder Packet { get; }
        public DateTime Time => Packet.BaseData.Time.ToDateTime();
        public int Diff { get; set;  }
        public uint Entry { get; }
        public string? ObjectName { get; }
        
        public PacketViewModel(PacketHolder packet, uint entry, string? objectName)
        {
            Packet = packet;
            Entry = entry;
            ObjectName = objectName;
        }
    }
}