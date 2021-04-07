using System;
using System.Diagnostics;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    public class ReportBugService : IReportBugService
    {
        public void ReportBug()
        {
            OpenUrl("https://github.com/BAndysc/WoWDatabaseEditor/issues/new?assignees=&labels=bug&template=bug_report.md&title=");
        }

        public void SendFeedback()
        {
            OpenUrl("https://github.com/BAndysc/WoWDatabaseEditor/discussions/");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) {UseShellExecute = true});
            }
            catch (Exception _)
            {
                
            }
        }
    }
    
    public interface IReportBugService
    {
        void ReportBug();
        void SendFeedback();
    }
}