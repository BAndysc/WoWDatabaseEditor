using System.Threading.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Processing.ProcessorProviders
{
    [NonUniqueProvider]
    public interface IPacketDumperProvider
    {
        string Name { get; }
        string Description { get; }
        ImageUri? Image => null;
        bool RequiresSplitUpdateObject => false;
        bool CanProcessMultipleFiles => false;
    }
    
    [NonUniqueProvider]
    public interface ITextPacketDumperProvider : IPacketDumperProvider
    {
        string Extension { get; }
        Task<IPacketTextDumper> CreateDumper(IParsingSettings settings);
    }
    
    [NonUniqueProvider]
    public interface IDocumentPacketDumperProvider : IPacketDumperProvider
    {
        Task<IPacketDocumentDumper> CreateDumper(IParsingSettings settings);
    }
}