using System;
using System.Threading.Tasks;
using Prism.Mvvm;
using WDE.Common.Types;
using WDE.PacketViewer.Processing;
using WDE.PacketViewer.Processing.ProcessorProviders;

namespace WDE.PacketViewer.ViewModels
{
    public class ProcessorViewModel : BindableBase
    {
        private readonly IPacketDumperProvider dumperProvider;
        private readonly ITextPacketDumperProvider? textProvider;
        private readonly IDocumentPacketDumperProvider? documentProvider;
        private bool isChecked;

        public ProcessorViewModel(IPacketDumperProvider dumperProvider)
        {
            this.dumperProvider = dumperProvider;
            if (dumperProvider is ITextPacketDumperProvider textProvider)
            {
                this.textProvider = textProvider;
                Extension = textProvider.Extension;
            }
            else if (dumperProvider is IDocumentPacketDumperProvider documentProvider)
            {
                this.documentProvider = documentProvider;
            }
            else
                throw new Exception($"Unexpected dumper type (neither text dumper not document dumper, got: {dumperProvider.GetType()})");
        }

        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }

        public string? Extension { get; }
        public string Name => dumperProvider.Name;
        public string? Description => dumperProvider.Description;
        public string? Format => Extension == "txt" ? "text" : Extension;
        public ImageUri Image => dumperProvider.Image ?? new ImageUri("Icons/document_sniff.png");
        public Task<IPacketTextDumper> CreateTextProcessor(IParsingSettings settings) => textProvider!.CreateDumper(settings);
        public Task<IPacketDocumentDumper> CreateDocumentProcessor(IParsingSettings settings) => documentProvider!.CreateDumper(settings);
        public bool IsTextDumper => textProvider != null;
        public bool IsDocumentDumper => documentProvider != null;
    }
}