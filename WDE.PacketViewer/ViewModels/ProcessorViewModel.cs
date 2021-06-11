using System;
using Prism.Mvvm;
using WDE.PacketViewer.Processing;

namespace WDE.PacketViewer.ViewModels
{
    public class ProcessorViewModel : BindableBase
    {
        private readonly Func<IPacketToTextProcessor> processorCreator;
        private bool isChecked;

        public ProcessorViewModel(string name, string? description, Func<IPacketToTextProcessor> processorCreator)
        {
            this.processorCreator = processorCreator;
            Name = name;
            Description = description;
        }

        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }
        public string Name { get; }
        public string? Description { get; }
        public IPacketToTextProcessor GetProcessor() => processorCreator();
    }
}