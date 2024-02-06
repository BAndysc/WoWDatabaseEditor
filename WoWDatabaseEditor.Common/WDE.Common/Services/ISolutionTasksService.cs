using System.ComponentModel;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface ISolutionTasksService : INotifyPropertyChanged
    {
        Task SaveSolutionToDatabaseTask(ISolutionItem item);
        Task SaveSolutionToDatabaseTask(ISolutionItemDocument document);
        Task ReloadSolutionRemotelyTask(ISolutionItem item);
        Task SaveAndReloadSolutionTask(ISolutionItem item);
        Task Save(ISolutionItemDocument document);
        
        bool CanSaveToDatabase { get; }
        bool CanReloadRemotely { get; }
        bool CanSaveAndReloadRemotely { get; }
    }
}