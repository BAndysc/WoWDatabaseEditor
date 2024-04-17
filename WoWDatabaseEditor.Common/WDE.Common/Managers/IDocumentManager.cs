using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
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
        IDocument? OpenDocument(ISolutionItem item);
        void OpenTool<T>() where T : ITool;
        T GetTool<T>() where T : ITool;
        void OpenTool(Type toolType);
        Task<bool> TryCloseAllDocuments(bool closingEditor);
        void ActivateDocumentInTheBackground(IDocument document);
        // Indicated whether active document should be brought to front (False) or not (True)
        bool BackgroundMode { get; }
    }

    public static class DocumentManagerExtensions
    {
        public static T? TryFindDocumentEditor<T>(this IDocumentManager documentManager, Func<T, bool> selector) where T : class
        {
            foreach (var docu in documentManager.OpenedDocuments)
            {
                if (docu is T t)
                {
                    if (selector(t))
                        return t;
                }
            }

            return null;
        }
        
        public static ISolutionItemDocument? TryFindDocument(this IDocumentManager documentManager, ISolutionItem item)
        {
            foreach (var docu in documentManager.OpenedDocuments)
            {
                if (docu is ISolutionItemDocument solutionItemDocument)
                {
                    if (solutionItemDocument.SolutionItem.Equals(item))
                        return solutionItemDocument;
                }
            }

            return null;
        }
    }
}