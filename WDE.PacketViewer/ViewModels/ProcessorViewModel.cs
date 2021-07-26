using System;
using System.Threading.Tasks;
using Prism.Mvvm;
using WDE.PacketViewer.Processing;
using WDE.PacketViewer.Processing.ProcessorProviders;

namespace WDE.PacketViewer.ViewModels
{
    public class ProcessorViewModel : BindableBase
    {
        private readonly IPacketDumperProvider dumperProvider;
        private bool isChecked;

        public ProcessorViewModel(IPacketDumperProvider dumperProvider)
        {
            this.dumperProvider = dumperProvider;
        }

        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }

        public string Name => dumperProvider.Name;
        public string Extension => dumperProvider.Extension;
        public string? Description => dumperProvider.Description;
        public Task<IPacketTextDumper> CreateProcessor() => dumperProvider.CreateDumper();
    }
}