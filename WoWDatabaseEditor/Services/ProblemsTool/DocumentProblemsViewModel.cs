using System.Collections.ObjectModel;
using WDE.Common.Managers;

namespace WoWDatabaseEditorCore.Services.ProblemsTool
{
    public class DocumentProblemsViewModel : ObservableCollection<ProblemViewModel>
    {
        public string Name { get; }
        public IDocument Document { get; }
        public bool Added { get; set; }
        public System.IDisposable? Subscription { get; set; }

        public DocumentProblemsViewModel(IDocument document)
        {
            Document = document;
            Name = document.Title;
        }
    }
}