using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Annotations;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IRemoteConnectorService
    {
        bool IsConnected { get; }
        
        Task<string> ExecuteCommand(IRemoteCommand command);
        Task ExecuteCommands(IList<IRemoteCommand> commands);

        IList<IRemoteCommand> Merge(IList<IRemoteCommand> commands);
    }

    public interface IRemoteCommand
    {
        string GenerateCommand();
        bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand);
    }
}