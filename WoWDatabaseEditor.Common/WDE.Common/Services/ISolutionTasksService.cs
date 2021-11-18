using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface ISolutionTasksService
    {
        Task SaveSolutionToDatabaseTask(ISolutionItem item);
        Task ReloadSolutionRemotelyTask(ISolutionItem item);
        Task SaveAndReloadSolutionTask(ISolutionItem item);
        
        bool CanSaveToDatabase { get; }
        bool CanReloadRemotely { get; }
        bool CanSaveAndReloadRemotely { get; }
    }
}