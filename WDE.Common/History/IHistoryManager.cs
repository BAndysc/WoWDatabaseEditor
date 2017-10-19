using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.History
{
    public interface IHistoryManager : INotifyPropertyChanged
    {
        bool CanUndo { get; }
        bool CanRedo { get; }

        int UndoCount { get; }
        int RedoCount { get; }

        void Redo();
        void Undo();

        void AddHandler(HistoryHandler handler);
    }
}
