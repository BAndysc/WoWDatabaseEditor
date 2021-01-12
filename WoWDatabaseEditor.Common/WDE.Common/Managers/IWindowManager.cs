using System.Collections.ObjectModel;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IWindowManager
    {
        IDocument ActiveDocument { get; set; }

        ObservableCollection<IDocument> OpenedDocuments { get; }

        ObservableCollection<ITool> OpenedTools { get; }
        void OpenDocument(IDocument editor);
        void OpenTool(IToolProvider provider);
    }
}