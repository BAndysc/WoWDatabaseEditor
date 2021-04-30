using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WDE.Common.History
{
    public interface IHistoryManager : INotifyPropertyChanged
    {
        bool CanUndo { get; }
        bool CanRedo { get; }
        bool IsSaved { get; }

        int UndoCount { get; }
        int RedoCount { get; }

        ObservableCollection<IHistoryAction> Past { get; }
        ObservableCollection<IHistoryAction> Future { get; }

        void Redo();
        void Undo();
        void MarkAsSaved();
        void MarkNoSave();

        void AddHandler(HistoryHandler handler);
    }
}