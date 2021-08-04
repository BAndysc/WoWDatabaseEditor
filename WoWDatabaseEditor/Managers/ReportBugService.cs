using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.Http;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    public class ReportBugService : IReportBugService
    {
        private readonly IUrlOpenService urlOpenService;

        public ReportBugService(IUrlOpenService urlOpenService)
        {
            this.urlOpenService = urlOpenService;
        }
        
        public void ReportBug()
        {
            urlOpenService.OpenUrl("https://github.com/BAndysc/WoWDatabaseEditor/issues/new?assignees=&labels=bug&template=bug_report.md&title=");
        }

        public void SendFeedback()
        {
            urlOpenService.OpenUrl("https://github.com/BAndysc/WoWDatabaseEditor/discussions/");
        }
    }
    
    public interface IReportBugService
    {
        void ReportBug();
        void SendFeedback();
    }
}