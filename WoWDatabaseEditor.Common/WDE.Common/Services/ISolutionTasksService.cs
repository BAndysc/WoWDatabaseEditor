using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface ISolutionTasksService
    {
        void SaveSolutionToDatabaseTask(ISolutionItem item);
        void ReloadSolutionRemotelyTask(ISolutionItem item);
        void SaveAndReloadSolutionTask(ISolutionItem item);
        
        bool CanSaveToDatabase { get; }
        bool CanReloadRemotely { get; }
        bool CanSaveAndReloadRemotely { get; }
    }
}