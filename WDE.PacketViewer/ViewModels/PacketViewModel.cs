using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public class PacketViewModel : INotifyPropertyChanged
    {
        private int diff;
        public int OriginalId { get; }
        public int Id => Packet.BaseData.Number;
        public string Opcode => Packet.BaseData.Opcode;
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
        
        public PacketViewModel(PacketHolder packet, int originalId, uint entry, UniversalGuid? mainActor, string? objectName)
        {
            OriginalId = originalId;
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