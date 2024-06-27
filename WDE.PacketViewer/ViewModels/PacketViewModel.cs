using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public unsafe class PacketViewModel //: INotifyPropertyChanged
    {
        private int diff;
        public int OriginalId { get; }
        public int Id => Packet.BaseData.Number;
        private string? cachedOpcode;
        public string Opcode => cachedOpcode ??= Packet.BaseData.Opcode.ToString() ?? "";
        private PacketHolder* packet;
        public ref PacketHolder Packet => ref *packet;
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
        public PacketHolder* PacketPtr => packet;

        public PacketViewModel(PacketHolder* packet, int originalId, uint entry, UniversalGuid? mainActor, string? objectName)
        {
            OriginalId = originalId;
            this.packet = packet;
            Entry = entry;
            MainActor = mainActor;
            ObjectName = objectName;
        }

//        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
  //          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}