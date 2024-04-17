using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Prism.Commands;

namespace WDE.Common.History
{
    public interface IHistoryManager : INotifyPropertyChanged
    {
        bool CanUndo { get; }
        bool CanRedo { get; }
        bool IsSaved { get; }

        int UndoCount { get; }
        int RedoCount { get; }
        bool IsUndoing { get; }

        ObservableCollection<IHistoryAction> Past { get; }
        ObservableCollection<IHistoryAction> Future { get; }

        void Redo();
        void Undo();
        void MarkAsSaved();
        void MarkNoSave();

        T AddHandler<T>(T handler) where T : HistoryHandler;
        void RemoveHandler(HistoryHandler handler);
        void Clear();
        void LimitStack(int limit);

        event Action? OnUndo;
        event Action? OnRedo;
    }

    public static class HistoryExtension
    {
        public static ICommand UndoCommand(this IHistoryManager historyManager)
        {
            var cmd = new DelegateCommand(historyManager.Undo, () => historyManager.CanUndo);
            historyManager.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(historyManager.CanUndo))
                    cmd.RaiseCanExecuteChanged();
            };
            return cmd;
        }
        
        public static ICommand RedoCommand(this IHistoryManager historyManager)
        {
            var cmd = new DelegateCommand(historyManager.Redo, () => historyManager.CanRedo);
            historyManager.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(historyManager.CanRedo))
                    cmd.RaiseCanExecuteChanged();
            };

            return cmd;
        }
    }
}