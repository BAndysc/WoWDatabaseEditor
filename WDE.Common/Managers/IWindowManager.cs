using System.Collections.ObjectModel;
using WDE.Module.Attributes;
using WDE.Common.Windows;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IWindowManager
    {
        Document ActiveDocument { get; set; }
        
        ObservableCollection<Document> OpenedDocuments { get; }
        void OpenDocument(Document editor);
        
        ObservableCollection<Document> OpenedTools { get; }
        void OpenTool(IToolProvider provider);
    }
}