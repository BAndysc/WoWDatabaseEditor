using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing
{
    public interface IPacketTextDumper : IPacketProcessor<bool>
    {
        bool RequiresSplitUpdateObject => false;
        Task Process() => Task.CompletedTask;
        Task<string> Generate();
    }
    
    public interface IPacketDocumentDumper : IPacketProcessor<bool>
    {
        bool RequiresSplitUpdateObject => false;
        Task Process() => Task.CompletedTask;
        Task<IDocument> Generate(PacketDocumentViewModel? packetDocumentViewModel);
    }

    public interface IPerFileStateProcessor : IPacketProcessor<bool>
    {
        void ClearAllState();
    }

    public interface ITwoStepPacketBoolProcessor : ITwoStepPacketProcessor<bool>
    {
    }
    
    public interface IUnfilteredTwoStepPacketBoolProcessor : IUnfilteredTwoStepPacketProcessor<bool>
    {
    }
    
    public interface ITwoStepPacketProcessor<T>
    {
        T? PreProcess(ref readonly PacketHolder packet);
        Task PostProcessFirstStep() => Task.CompletedTask;
    }

    public interface IUnfilteredTwoStepPacketProcessor<T>
    {
        T? UnfilteredPreProcess(ref readonly PacketHolder packet);
    }

    public interface IUnfilteredPacketProcessor
    {
        void ProcessUnfiltered(ref PacketHolder unfiltered);
    }

    public interface INeedToPostProcess
    {
        Task PostProcess();
    }
}