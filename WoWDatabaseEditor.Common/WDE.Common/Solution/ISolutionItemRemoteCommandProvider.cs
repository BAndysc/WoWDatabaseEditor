using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemRemoteCommandProvider
    {
    }

    [NonUniqueProvider]
    public interface ISolutionItemRemoteCommandProvider<T> : ISolutionItemRemoteCommandProvider where T : ISolutionItem
    {
        IRemoteCommand[] GenerateCommand(T item);
    }
}