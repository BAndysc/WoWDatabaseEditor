using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [UniqueProvider]
    public interface ISolutionItemRemoteCommandGeneratorRegistry
    {
        IRemoteCommand[] GenerateCommand(ISolutionItem item);
    }
}