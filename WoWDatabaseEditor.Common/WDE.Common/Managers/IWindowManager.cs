using System.Collections.ObjectModel;
using WDE.Module.Attributes;
using WDE.Common.Windows;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IWindowManager
    {
        IDocument ActiveDocument { get; set; }
        
        ObservableCollection<IDocument> OpenedDocuments { get; }
        void OpenDocument(IDocument editor);
        
        ObservableCollection<ITool> OpenedTools { get; }
        void OpenTool(IToolProvider provider);
    }
}