using System.Collections.ObjectModel;
using WDE.Common.Windows;

namespace WDE.Common.Managers
{
    public interface IWindowManager
    {
        DocumentEditor ActiveDocument { get; set; }
        
        ObservableCollection<DocumentEditor> OpenedDocuments { get; }
        void OpenDocument(DocumentEditor editor);
        
        ObservableCollection<DocumentEditor> OpenedWindows { get; }
        void OpenWindow(IWindowProvider provider);
    }
}