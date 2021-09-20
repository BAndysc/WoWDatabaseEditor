using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public class PacketViewModel : INotifyPropertyChanged
    {
        private int diff;
        public int Id => Packet.BaseData.Number;
        public string Opcode => Packet.BaseData.Opcode;
        public string Text => Packet.BaseData.StringData;
        public PacketHolder Packet { get; }
        public DateTime Time => Packet.BaseData.Time.ToDateTime();

        public int Diff
        {
            get => diff;
            set
            {
                diff = value;
                OnPropertyChanged();
            }
        }

        public uint Entry { get; }
        public UniversalGuid? MainActor { get; }
        public string? ObjectName { get; }
        
        public PacketViewModel(PacketHolder packet, uint entry, UniversalGuid? mainActor, string? objectName)
        {
            Packet = packet;
            Entry = entry;
            MainActor = mainActor;
            ObjectName = objectName;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}