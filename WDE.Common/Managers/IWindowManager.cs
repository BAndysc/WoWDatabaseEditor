using System.Collections.ObjectModel;
using WDE.Common.Attributes;
using WDE.Common.Windows;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IWindowManager
    {
        DocumentEditor ActiveDocument { get; set; }
        
        ObservableCollection<DocumentEditor> OpenedDocuments { get; }
        void OpenDocument(DocumentEditor editor);
        
        ObservableCollection<DocumentEditor> OpenedTools { get; }
        void OpenTool(IToolProvider provider);
    }
}