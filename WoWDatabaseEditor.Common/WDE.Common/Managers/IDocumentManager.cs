using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IDocumentManager : INotifyPropertyChanged
    {
        IDocument? ActiveDocument { get; set; }
        IFocusableTool? ActiveTool { get; set; }
        IUndoRedoWindow? ActiveUndoRedo { get; }
        ISolutionItemDocument? ActiveSolutionItemDocument { get; }
        IReadOnlyList<ITool> AllTools { get; }
        ObservableCollection<IDocument> OpenedDocuments { get; }
        ObservableCollection<ITool> OpenedTools { get; }
        ITool? SelectedTool { get; set; }
        void OpenDocument(IDocument editor);
        void OpenTool<T>() where T : ITool;
        T GetTool<T>() where T : ITool;
        void OpenTool(Type toolType);
        void ActivateDocumentInTheBackground(IDocument document);
        // Indicated whether active document should be brought to front (False) or not (True)
        bool BackgroundMode { get; }
    }
}